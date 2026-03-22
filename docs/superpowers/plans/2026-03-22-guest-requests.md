# Guest Requests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow team leaders to create parking requests for guests, with highest allocation priority.

**Architecture:** New `GuestRequest` model stored in DynamoDB (PK: GLOBAL, SK: GUESTS#YYYY-MM). New `IGuestRequestRepository` in Business.Data, implemented in Data layer. New `GuestRequestsController` with CRUD endpoints. Modify `AllocationCreator` and `RequestUpdater` to handle guest allocation. Modify `DailyDetailsController` and `RegistrationNumbersController` to include guests.

**Tech Stack:** .NET 10, ASP.NET Core, DynamoDB (AWS SDK), NodaTime, xUnit, Moq

**Spec:** `docs/superpowers/specs/2026-03-22-guest-requests-design.md`

---

## File Structure

### New Files
- `Parking.Model/GuestRequest.cs` - Domain model
- `Parking.Model/GuestRequestStatus.cs` - Status enum (Pending, Allocated, Interrupted)
- `Parking.Business/Data/IGuestRequestRepository.cs` - Repository interface
- `Parking.Data/GuestRequestRepository.cs` - DynamoDB implementation
- `Parking.Api/Controllers/GuestRequestsController.cs` - CRUD endpoints
- `Parking.Api/Json/GuestRequests/GuestRequestsPostRequest.cs` - POST request body
- `Parking.Api/Json/GuestRequests/GuestRequestsPutRequest.cs` - PUT request body
- `Parking.Api/Json/GuestRequests/GuestRequestsResponse.cs` - GET response
- `Parking.Api/Json/GuestRequests/GuestRequestsData.cs` - Single item in GET response
- `Parking.Business/AllocationResult.cs` - Return type from AllocationCreator
- `Parking.TestHelpers/GuestRequestRepositoryBuilder.cs` - Test helper
- `Parking.Api.UnitTests/Controllers/GuestRequestsControllerTests.cs` - Controller tests
- `Parking.Business.UnitTests/AllocationCreatorGuestTests.cs` - Allocation tests for guests
- `Parking.Business.UnitTests/RequestUpdaterGuestTests.cs` - RequestUpdater guest tests
- `Parking.Data.UnitTests/GuestRequestRepositoryTests.cs` - Repository unit tests (if applicable)

### Modified Files
- `Parking.Data/RawItem.cs` - Add Guests property, GuestsConverter, CreateGuests factory
- `Parking.Data/Aws/DatabaseProvider.cs` - Add GetGuests query method
- `Parking.Business/AllocationCreator.cs` - Accept and allocate guest requests first
- `Parking.Business/RequestUpdater.cs` - Fetch/pass/persist guest requests
- `Parking.Api/Controllers/DailyDetailsController.cs` - Include guests in daily details
- `Parking.Api/Controllers/RegistrationNumbersController.cs` - Include guest registration numbers
- `Parking.Api/Startup.cs` - Register IGuestRequestRepository
- `Parking.Service/Startup.cs` - Register IGuestRequestRepository
- `Parking.Api.UnitTests/Controllers/DailyDetailsControllerTests.cs` - Add guest tests
- `Parking.Api.UnitTests/Controllers/RegistrationNumbersControllerTests.cs` - Add guest tests

---

## Task 1: GuestRequest Model and Status Enum

**Files:**
- Create: `Parking.Model/GuestRequestStatus.cs`
- Create: `Parking.Model/GuestRequest.cs`

- [ ] **Step 1: Create GuestRequestStatus enum**

```csharp
namespace Parking.Model;

public enum GuestRequestStatus
{
    Allocated,
    Interrupted,
    Pending,
}
```

- [ ] **Step 2: Create GuestRequest model**

```csharp
namespace Parking.Model;

using NodaTime;

public class GuestRequest(
    string id,
    LocalDate date,
    string name,
    string visitingUserId,
    string? registrationNumber,
    GuestRequestStatus status)
{
    public string Id { get; } = id;

    public LocalDate Date { get; } = date;

    public string Name { get; } = name;

    public string VisitingUserId { get; } = visitingUserId;

    public string? RegistrationNumber { get; } = registrationNumber;

    public GuestRequestStatus Status { get; } = status;
}
```

- [ ] **Step 3: Build to verify compilation**

Run: `dotnet build Parking.Model`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Parking.Model/GuestRequest.cs Parking.Model/GuestRequestStatus.cs
git commit -m "Add GuestRequest model and GuestRequestStatus enum"
```

---

## Task 2: IGuestRequestRepository Interface

**Files:**
- Create: `Parking.Business/Data/IGuestRequestRepository.cs`

- [ ] **Step 1: Create the interface**

```csharp
namespace Parking.Business.Data;

using System.Collections.Generic;
using System.Threading.Tasks;
using Model;
using NodaTime;

public interface IGuestRequestRepository
{
    Task<IReadOnlyCollection<GuestRequest>> GetGuestRequests(DateInterval dateInterval);

    Task SaveGuestRequest(GuestRequest guestRequest);

    Task SaveGuestRequests(IReadOnlyCollection<GuestRequest> guestRequests);

    Task UpdateGuestRequest(GuestRequest guestRequest);

    Task DeleteGuestRequest(LocalDate date, string id);
}
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build Parking.Business`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Parking.Business/Data/IGuestRequestRepository.cs
git commit -m "Add IGuestRequestRepository interface"
```

---

## Task 3: DynamoDB Storage - RawItem and Converter

**Files:**
- Modify: `Parking.Data/RawItem.cs`
- Modify: `Parking.Data/Aws/DatabaseProvider.cs`

- [ ] **Step 1: Add GuestData class, GuestsConverter, and Guests property to RawItem**

In `Parking.Data/RawItem.cs`, add a `GuestData` class to represent a single guest in the DynamoDB storage:

```csharp
public class GuestData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string VisitingUserId { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string Status { get; set; } = string.Empty;
}
```

Add a `GuestsConverter` class (similar pattern to `ReservationsConverter`):

```csharp
public class GuestsConverter : IPropertyConverter
{
    public object FromEntry(DynamoDBEntry entry) =>
        entry.AsDocument().ToDictionary(
            dailyData => dailyData.Key,
            dailyData => dailyData.Value
                .AsDynamoDBList()
                .Entries
                .Where(e => e is Document)
                .Select(e =>
                {
                    var doc = e.AsDocument();
                    return new GuestData
                    {
                        Id = doc.ContainsKey("id") ? doc["id"].AsString() : string.Empty,
                        Name = doc.ContainsKey("name") ? doc["name"].AsString() : string.Empty,
                        VisitingUserId = doc.ContainsKey("visitingUserId") ? doc["visitingUserId"].AsString() : string.Empty,
                        RegistrationNumber = doc.ContainsKey("registrationNumber") ? doc["registrationNumber"].AsString() : null,
                        Status = doc.ContainsKey("status") ? doc["status"].AsString() : string.Empty,
                    };
                })
                .ToList());

    public DynamoDBEntry ToEntry(object value)
    {
        if (value is not Dictionary<string, List<GuestData>> dailyData)
        {
            throw new ArgumentException("Could not convert raw value to dictionary", nameof(value));
        }

        return new Document(
            dailyData.ToDictionary(
                day => day.Key,
                day => (DynamoDBEntry)new DynamoDBList(day.Value.Select(guest =>
                {
                    var doc = new Document();
                    doc["id"] = new Primitive(guest.Id);
                    doc["name"] = new Primitive(guest.Name);
                    doc["visitingUserId"] = new Primitive(guest.VisitingUserId);
                    if (guest.RegistrationNumber != null)
                    {
                        doc["registrationNumber"] = new Primitive(guest.RegistrationNumber);
                    }
                    doc["status"] = new Primitive(guest.Status);
                    return (DynamoDBEntry)doc;
                }))));
    }
}
```

Add `CreateGuests` factory method to `RawItem`:

```csharp
public static RawItem CreateGuests(
    string primaryKey,
    string sortKey,
    Dictionary<string, List<GuestData>> guests) => new RawItem(primaryKey, sortKey)
    {
        Guests = guests
    };
```

Add `Guests` property to `RawItem`:

```csharp
[DynamoDBProperty("guests", typeof(GuestsConverter))]
public Dictionary<string, List<GuestData>>? Guests { get; set; }
```

- [ ] **Step 2: Add GetGuests method to IDatabaseProvider and DatabaseProvider**

In `IDatabaseProvider`, add:

```csharp
Task<IReadOnlyCollection<RawItem>> GetGuests(YearMonth yearMonth);
```

In `DatabaseProvider`, implement:

```csharp
public async Task<IReadOnlyCollection<RawItem>> GetGuests(YearMonth yearMonth)
{
    const string HashKeyValue = "GLOBAL";
    var conditionValue = $"GUESTS#{YearMonthPattern.Iso.Format(yearMonth)}";

    return await this.QueryPartitionKey(HashKeyValue, conditionValue);
}
```

- [ ] **Step 3: Build to verify compilation**

Run: `dotnet build Parking.Data`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Parking.Data/RawItem.cs Parking.Data/Aws/DatabaseProvider.cs
git commit -m "Add DynamoDB storage for guest requests"
```

---

## Task 4: GuestRequestRepository Implementation

**Files:**
- Create: `Parking.Data/GuestRequestRepository.cs`

- [ ] **Step 1: Implement GuestRequestRepository**

Follow the pattern from `ReservationRepository.cs`. Key methods:

```csharp
namespace Parking.Data;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Aws;
using Business.Data;
using Microsoft.Extensions.Logging;
using Model;
using NodaTime;

public class GuestRequestRepository(
    ILogger<GuestRequestRepository> logger,
    IDatabaseProvider databaseProvider) : IGuestRequestRepository
{
    public async Task<IReadOnlyCollection<GuestRequest>> GetGuestRequests(DateInterval dateInterval)
    {
        var matchingGuestRequests = new List<GuestRequest>();

        foreach (var yearMonth in dateInterval.YearMonths())
        {
            var queryResult = await databaseProvider.GetGuests(yearMonth);

            var wholeMonthGuests = queryResult.SelectMany(r => CreateWholeMonthGuestRequests(r, yearMonth));

            matchingGuestRequests.AddRange(wholeMonthGuests.Where(g => dateInterval.Contains(g.Date)));
        }

        return matchingGuestRequests;
    }

    public async Task SaveGuestRequest(GuestRequest guestRequest)
    {
        await SaveGuestRequests([guestRequest]);
    }

    public async Task SaveGuestRequests(IReadOnlyCollection<GuestRequest> guestRequests)
    {
        if (!guestRequests.Any())
        {
            return;
        }

        logger.LogDebug(
            "Saving guest requests: {@guestRequests}",
            guestRequests.Select(g => new { g.Id, g.Name, g.Date, g.Status }));

        var yearMonths = guestRequests.Select(g => g.Date.ToYearMonth()).Distinct();

        foreach (var yearMonth in yearMonths)
        {
            var existingGuests = await GetGuestRequestsForMonth(yearMonth);

            var monthGuests = guestRequests.Where(g => g.Date.ToYearMonth() == yearMonth).ToList();

            var combinedGuests = existingGuests
                .Where(existing => !monthGuests.Any(g => g.Id == existing.Id))
                .Concat(monthGuests)
                .ToList();

            var rawItem = CreateRawItem(yearMonth, combinedGuests);
            await databaseProvider.SaveItem(rawItem);
        }
    }

    public async Task UpdateGuestRequest(GuestRequest guestRequest)
    {
        await SaveGuestRequests([guestRequest]);
    }

    public async Task DeleteGuestRequest(LocalDate date, string id)
    {
        var yearMonth = date.ToYearMonth();
        var existingGuests = await GetGuestRequestsForMonth(yearMonth);

        var updatedGuests = existingGuests.Where(g => g.Id != id).ToList();

        var rawItem = CreateRawItem(yearMonth, updatedGuests);
        await databaseProvider.SaveItem(rawItem);
    }

    private async Task<IReadOnlyCollection<GuestRequest>> GetGuestRequestsForMonth(YearMonth yearMonth)
    {
        var firstDate = yearMonth.OnDayOfMonth(1);
        var lastDate = yearMonth.OnDayOfMonth(CalendarSystem.Iso.GetDaysInMonth(yearMonth.Year, yearMonth.Month));
        var dateInterval = new DateInterval(firstDate, lastDate);

        return await GetGuestRequests(dateInterval);
    }

    private static IEnumerable<GuestRequest> CreateWholeMonthGuestRequests(RawItem rawItem, YearMonth yearMonth)
    {
        var wholeMonthRawGuests = rawItem.Guests;

        if (wholeMonthRawGuests == null)
        {
            throw new InvalidOperationException("Raw guests cannot be null.");
        }

        return wholeMonthRawGuests.SelectMany(
            singleDayRawGuests => CreateSingleDayGuestRequests(yearMonth, singleDayRawGuests));
    }

    private static IEnumerable<GuestRequest> CreateSingleDayGuestRequests(
        YearMonth yearMonth,
        KeyValuePair<string, List<GuestData>> singleDayRawGuests)
    {
        var date = new LocalDate(yearMonth.Year, yearMonth.Month, int.Parse(singleDayRawGuests.Key));

        return singleDayRawGuests.Value.Select(guest => new GuestRequest(
            id: guest.Id,
            date: date,
            name: guest.Name,
            visitingUserId: guest.VisitingUserId,
            registrationNumber: guest.RegistrationNumber,
            status: CreateGuestRequestStatus(guest.Status)));
    }

    private static GuestRequestStatus CreateGuestRequestStatus(string rawStatus) =>
        rawStatus switch
        {
            "A" => GuestRequestStatus.Allocated,
            "I" => GuestRequestStatus.Interrupted,
            "P" => GuestRequestStatus.Pending,
            _ => throw new ArgumentException($"Unrecognised guest request status: {rawStatus}")
        };

    private static string CreateRawGuestRequestStatus(GuestRequestStatus status) =>
        status switch
        {
            GuestRequestStatus.Allocated => "A",
            GuestRequestStatus.Interrupted => "I",
            GuestRequestStatus.Pending => "P",
            _ => throw new ArgumentException($"Unrecognised guest request status: {status}")
        };

    private static RawItem CreateRawItem(YearMonth yearMonth, IReadOnlyCollection<GuestRequest> guestRequests)
    {
        var rawGuests = guestRequests
            .GroupBy(g => g.Date)
            .ToDictionary(
                g => g.Key.Day.ToString("D2", CultureInfo.InvariantCulture),
                g => g.Select(guest => new GuestData
                {
                    Id = guest.Id,
                    Name = guest.Name,
                    VisitingUserId = guest.VisitingUserId,
                    RegistrationNumber = guest.RegistrationNumber,
                    Status = CreateRawGuestRequestStatus(guest.Status),
                }).ToList());

        return RawItem.CreateGuests(
            primaryKey: "GLOBAL",
            sortKey: $"GUESTS#{yearMonth.ToString("yyyy-MM", CultureInfo.InvariantCulture)}",
            guests: rawGuests);
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build Parking.Data`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Parking.Data/GuestRequestRepository.cs
git commit -m "Add GuestRequestRepository implementation"
```

---

## Task 5: GuestRequestRepositoryBuilder Test Helper

**Files:**
- Create: `Parking.TestHelpers/GuestRequestRepositoryBuilder.cs`

- [ ] **Step 1: Create the builder**

Follow the pattern from `ReservationRepositoryBuilder.cs`:

```csharp
namespace Parking.TestHelpers;

using System.Collections.Generic;
using Business.Data;
using Model;
using Moq;
using NodaTime;

public class GuestRequestRepositoryBuilder
{
    private readonly Mock<IGuestRequestRepository> mockGuestRequestRepository = new();

    public GuestRequestRepositoryBuilder WithGetGuestRequests(
        DateInterval dateInterval,
        IReadOnlyCollection<GuestRequest> guestRequests)
    {
        this.mockGuestRequestRepository
            .Setup(r => r.GetGuestRequests(dateInterval))
            .ReturnsAsync(guestRequests);

        return this;
    }

    public IGuestRequestRepository Build() => this.mockGuestRequestRepository.Object;

    public Mock<IGuestRequestRepository> BuildMock() => this.mockGuestRequestRepository;
}
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build Parking.TestHelpers`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Parking.TestHelpers/GuestRequestRepositoryBuilder.cs
git commit -m "Add GuestRequestRepositoryBuilder test helper"
```

---

## Task 6: Register IGuestRequestRepository in DI

**Files:**
- Modify: `Parking.Api/Startup.cs:89-96`
- Modify: `Parking.Service/Startup.cs:43-59`

- [ ] **Step 1: Register in Parking.Api/Startup.cs**

Add after the existing repository registrations (after line 96):

```csharp
services.AddScoped<IGuestRequestRepository, GuestRequestRepository>();
```

- [ ] **Step 2: Register in Parking.Service/Startup.cs**

Add after the existing repository registrations (near line 54):

```csharp
services.AddScoped<IGuestRequestRepository, GuestRequestRepository>();
```

- [ ] **Step 3: Build to verify compilation**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Parking.Api/Startup.cs Parking.Service/Startup.cs
git commit -m "Register IGuestRequestRepository in DI containers"
```

---

## Task 7: GuestRequestsController - POST Endpoint (TDD)

**Files:**
- Create: `Parking.Api/Json/GuestRequests/GuestRequestsPostRequest.cs`
- Create: `Parking.Api/Controllers/GuestRequestsController.cs`
- Create: `Parking.Api.UnitTests/Controllers/GuestRequestsControllerTests.cs`

### 7a: POST - Creates guest request successfully

- [ ] **Step 1: Create the POST request JSON model**

```csharp
namespace Parking.Api.Json.GuestRequests;

using NodaTime;

public class GuestRequestsPostRequest(
    LocalDate date,
    string name,
    string visitingUserId,
    string? registrationNumber)
{
    public LocalDate Date { get; } = date;

    public string Name { get; } = name;

    public string VisitingUserId { get; } = visitingUserId;

    public string? RegistrationNumber { get; } = registrationNumber;
}
```

- [ ] **Step 2: Write the failing test**

Create `Parking.Api.UnitTests/Controllers/GuestRequestsControllerTests.cs`:

```csharp
namespace Parking.Api.UnitTests.Controllers;

using System.Threading.Tasks;
using Api.Controllers;
using Api.Json.GuestRequests;
using Business;
using Business.Data;
using Microsoft.AspNetCore.Mvc;
using Model;
using Moq;
using NodaTime;
using NodaTime.Testing.Extensions;
using TestHelpers;
using Xunit;

public static class GuestRequestsControllerTests
{
    public static class Post
    {
        [Fact]
        public static async Task Creates_guest_request_successfully()
        {
            var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
            mockGuestRequestRepository
                .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
                .ReturnsAsync([]);
            mockGuestRequestRepository
                .Setup(r => r.SaveGuestRequest(It.Is<GuestRequest>(g =>
                    g.Date == 15.March(2026) &&
                    g.Name == "Alice Smith" &&
                    g.VisitingUserId == "user1" &&
                    g.RegistrationNumber == "AB12CDE" &&
                    g.Status == GuestRequestStatus.Pending &&
                    !string.IsNullOrEmpty(g.Id))))
                .Returns(Task.CompletedTask);

            var mockTriggerRepository = new Mock<ITriggerRepository>();

            var users = new[]
            {
                CreateUser.With(userId: "user1", firstName: "John", lastName: "Doe"),
            };

            var activeDates = new[] { 15.March(2026), 16.March(2026) };

            var controller = CreateController(
                guestRequestRepository: mockGuestRequestRepository.Object,
                triggerRepository: mockTriggerRepository.Object,
                userRepository: CreateUserRepository.WithUsers(users),
                activeDates: activeDates);

            var request = new GuestRequestsPostRequest(
                15.March(2026), "Alice Smith", "user1", "AB12CDE");

            var result = await controller.PostAsync(request);

            Assert.IsType<OkResult>(result);
            mockGuestRequestRepository.VerifyAll();
        }

        private static GuestRequestsController CreateController(
            IGuestRequestRepository? guestRequestRepository = null,
            ITriggerRepository? triggerRepository = null,
            IUserRepository? userRepository = null,
            IReadOnlyCollection<LocalDate>? activeDates = null)
        {
            var dateCalculator = CreateDateCalculator.WithActiveDates(
                activeDates ?? [15.March(2026)]);

            return new GuestRequestsController(
                dateCalculator,
                guestRequestRepository ?? Mock.Of<IGuestRequestRepository>(),
                triggerRepository ?? Mock.Of<ITriggerRepository>(),
                userRepository ?? Mock.Of<IUserRepository>());
        }
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test Parking.Api.UnitTests --filter "GuestRequestsControllerTests"`
Expected: FAIL - `GuestRequestsController` does not exist

- [ ] **Step 4: Create minimal GuestRequestsController with PostAsync**

```csharp
namespace Parking.Api.Controllers;

using System;
using System.Linq;
using System.Threading.Tasks;
using Business;
using Business.Data;
using Json.GuestRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Model;

[Authorize(Policy = "IsTeamLeader")]
[Route("guest-requests")]
[ApiController]
public class GuestRequestsController(
    IDateCalculator dateCalculator,
    IGuestRequestRepository guestRequestRepository,
    ITriggerRepository triggerRepository,
    IUserRepository userRepository)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PostAsync([FromBody] GuestRequestsPostRequest request)
    {
        var guestRequest = new GuestRequest(
            id: Guid.NewGuid().ToString(),
            date: request.Date,
            name: request.Name,
            visitingUserId: request.VisitingUserId,
            registrationNumber: request.RegistrationNumber,
            status: GuestRequestStatus.Pending);

        await guestRequestRepository.SaveGuestRequest(guestRequest);

        await triggerRepository.AddTrigger();

        return this.Ok();
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test Parking.Api.UnitTests --filter "Creates_guest_request_successfully"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add Parking.Api/Json/GuestRequests/GuestRequestsPostRequest.cs Parking.Api/Controllers/GuestRequestsController.cs Parking.Api.UnitTests/Controllers/GuestRequestsControllerTests.cs
git commit -m "Add POST /guest-requests endpoint - creates guest request"
```

### 7b: POST - Triggers allocation re-run

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public static async Task Triggers_allocation_rerun()
{
    var mockGuestRequestRepository = new Mock<IGuestRequestRepository>();
    mockGuestRequestRepository
        .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
        .ReturnsAsync([]);

    var mockTriggerRepository = new Mock<ITriggerRepository>(MockBehavior.Strict);
    mockTriggerRepository
        .Setup(r => r.AddTrigger())
        .Returns(Task.CompletedTask);

    var users = new[] { CreateUser.With(userId: "user1") };

    var controller = CreateController(
        guestRequestRepository: mockGuestRequestRepository.Object,
        triggerRepository: mockTriggerRepository.Object,
        userRepository: CreateUserRepository.WithUsers(users));

    var request = new GuestRequestsPostRequest(15.March(2026), "Guest", "user1", null);

    await controller.PostAsync(request);

    mockTriggerRepository.VerifyAll();
}
```

- [ ] **Step 2: Run test to verify it passes** (implementation already calls AddTrigger)

Run: `dotnet test Parking.Api.UnitTests --filter "Triggers_allocation_rerun"`
Expected: PASS (already implemented)

- [ ] **Step 3: Commit**

```bash
git add Parking.Api.UnitTests/Controllers/GuestRequestsControllerTests.cs
git commit -m "Add test: POST /guest-requests triggers allocation re-run"
```

### 7c: POST - Validation tests

- [ ] **Step 1: Write validation tests**

Add to the `Post` class. These tests validate: missing/empty name, non-existent user, deleted user, duplicate name on same date (case-insensitive), past date, date beyond end of subsequent month. Each test should verify a `BadRequest` or `NotFound` response.

Example for empty name:

```csharp
[Fact]
public static async Task Rejects_empty_name()
{
    var users = new[] { CreateUser.With(userId: "user1") };

    var controller = CreateController(
        userRepository: CreateUserRepository.WithUsers(users));

    var request = new GuestRequestsPostRequest(15.March(2026), "", "user1", null);

    var result = await controller.PostAsync(request);

    Assert.IsType<BadRequestResult>(result);
}
```

For duplicate name (case-insensitive):

```csharp
[Fact]
public static async Task Rejects_duplicate_name_on_same_date()
{
    var existingGuests = new[]
    {
        new GuestRequest("id1", 15.March(2026), "Alice Smith", "user1", null, GuestRequestStatus.Pending)
    };

    var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
    mockGuestRequestRepository
        .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
        .ReturnsAsync(existingGuests);

    var users = new[] { CreateUser.With(userId: "user1") };

    var controller = CreateController(
        guestRequestRepository: mockGuestRequestRepository.Object,
        userRepository: CreateUserRepository.WithUsers(users));

    var request = new GuestRequestsPostRequest(15.March(2026), "alice smith", "user1", null);

    var result = await controller.PostAsync(request);

    Assert.IsType<ConflictResult>(result);
}
```

For allows same name on different dates:

```csharp
[Fact]
public static async Task Allows_same_name_on_different_dates()
{
    var existingGuests = new[]
    {
        new GuestRequest("id1", 16.March(2026), "Alice Smith", "user1", null, GuestRequestStatus.Pending)
    };

    var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
    mockGuestRequestRepository
        .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
        .ReturnsAsync(existingGuests);
    mockGuestRequestRepository
        .Setup(r => r.SaveGuestRequest(It.IsAny<GuestRequest>()))
        .Returns(Task.CompletedTask);

    var users = new[] { CreateUser.With(userId: "user1") };

    var controller = CreateController(
        guestRequestRepository: mockGuestRequestRepository.Object,
        userRepository: CreateUserRepository.WithUsers(users),
        activeDates: [15.March(2026), 16.March(2026)]);

    var request = new GuestRequestsPostRequest(15.March(2026), "Alice Smith", "user1", null);

    var result = await controller.PostAsync(request);

    Assert.IsType<OkResult>(result);
}
```

For non-existent user:

```csharp
[Fact]
public static async Task Rejects_nonexistent_visiting_user()
{
    IReadOnlyCollection<User> users = [];

    var controller = CreateController(
        userRepository: CreateUserRepository.WithUsers(users));

    var request = new GuestRequestsPostRequest(15.March(2026), "Guest", "nonexistent", null);

    var result = await controller.PostAsync(request);

    Assert.IsType<BadRequestResult>(result);
}
```

For past date and beyond subsequent month: the controller will need to use `IDateCalculator` or `IClock` to determine the current date and month boundaries. Use the `activeDates` range to validate dates fall within the allowed range.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Parking.Api.UnitTests --filter "GuestRequestsControllerTests"`
Expected: FAIL - validation not yet implemented

- [ ] **Step 3: Add validation logic to PostAsync**

Update `PostAsync` to validate before saving:

```csharp
[HttpPost]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
public async Task<IActionResult> PostAsync([FromBody] GuestRequestsPostRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return this.BadRequest();
    }

    var activeDates = dateCalculator.GetActiveDates();

    if (!activeDates.Contains(request.Date))
    {
        return this.BadRequest();
    }

    var users = await userRepository.GetUsers();
    var visitingUser = users.SingleOrDefault(u => u.UserId == request.VisitingUserId);

    if (visitingUser == null)
    {
        return this.BadRequest();
    }

    var existingGuests = await guestRequestRepository.GetGuestRequests(request.Date.ToDateInterval());

    if (existingGuests.Any(g =>
        g.Date == request.Date &&
        string.Equals(g.Name, request.Name, StringComparison.OrdinalIgnoreCase)))
    {
        return this.Conflict();
    }

    var guestRequest = new GuestRequest(
        id: Guid.NewGuid().ToString(),
        date: request.Date,
        name: request.Name,
        visitingUserId: request.VisitingUserId,
        registrationNumber: request.RegistrationNumber,
        status: GuestRequestStatus.Pending);

    await guestRequestRepository.SaveGuestRequest(guestRequest);

    await triggerRepository.AddTrigger();

    return this.Ok();
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Parking.Api.UnitTests --filter "GuestRequestsControllerTests"`
Expected: PASS

- [ ] **Step 5: Add authorization test - non-team-leader gets 403**

This is handled by the `[Authorize(Policy = "IsTeamLeader")]` attribute on the controller. Add a test that verifies the attribute is present on the controller class (using reflection), following the pattern used elsewhere in the codebase if one exists. Otherwise, this is covered by the attribute and integration tests.

- [ ] **Step 6: Commit**

```bash
git add Parking.Api/Controllers/GuestRequestsController.cs Parking.Api.UnitTests/Controllers/GuestRequestsControllerTests.cs
git commit -m "Add POST /guest-requests validation"
```

---

## Task 8: GuestRequestsController - PUT Endpoint (TDD)

**Files:**
- Create: `Parking.Api/Json/GuestRequests/GuestRequestsPutRequest.cs`
- Modify: `Parking.Api/Controllers/GuestRequestsController.cs`
- Modify: `Parking.Api.UnitTests/Controllers/GuestRequestsControllerTests.cs`

- [ ] **Step 1: Create the PUT request JSON model**

```csharp
namespace Parking.Api.Json.GuestRequests;

public class GuestRequestsPutRequest(
    string name,
    string visitingUserId,
    string? registrationNumber)
{
    public string Name { get; } = name;

    public string VisitingUserId { get; } = visitingUserId;

    public string? RegistrationNumber { get; } = registrationNumber;
}
```

- [ ] **Step 2: Write the failing test for successful update**

Add a `Put` nested class to `GuestRequestsControllerTests`:

```csharp
public static class Put
{
    [Fact]
    public static async Task Updates_guest_request_successfully()
    {
        var existingGuest = new GuestRequest(
            "id1", 15.March(2026), "Alice Smith", "user1", null, GuestRequestStatus.Allocated);

        var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
        mockGuestRequestRepository
            .Setup(r => r.GetGuestRequests(15.March(2026).ToDateInterval()))
            .ReturnsAsync([existingGuest]);
        mockGuestRequestRepository
            .Setup(r => r.UpdateGuestRequest(It.Is<GuestRequest>(g =>
                g.Id == "id1" &&
                g.Name == "Alice Jones" &&
                g.VisitingUserId == "user2" &&
                g.RegistrationNumber == "XY34FGH" &&
                g.Status == GuestRequestStatus.Allocated)))
            .Returns(Task.CompletedTask);

        var users = new[]
        {
            CreateUser.With(userId: "user1"),
            CreateUser.With(userId: "user2"),
        };

        var controller = CreateController(
            guestRequestRepository: mockGuestRequestRepository.Object,
            userRepository: CreateUserRepository.WithUsers(users));

        var request = new GuestRequestsPutRequest("Alice Jones", "user2", "XY34FGH");

        var result = await controller.PutAsync("2026-03-15", "id1", request);

        Assert.IsType<OkResult>(result);
        mockGuestRequestRepository.VerifyAll();
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test Parking.Api.UnitTests --filter "Put"`
Expected: FAIL - `PutAsync` does not exist

- [ ] **Step 4: Implement PutAsync**

```csharp
[HttpPut("{date}/{id}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
public async Task<IActionResult> PutAsync(string date, string id, [FromBody] GuestRequestsPutRequest request)
{
    var localDate = LocalDatePattern.Iso.Parse(date);

    if (!localDate.Success)
    {
        return this.BadRequest();
    }

    var existingGuests = await guestRequestRepository.GetGuestRequests(localDate.Value.ToDateInterval());
    var existingGuest = existingGuests.SingleOrDefault(g => g.Id == id);

    if (existingGuest == null)
    {
        return this.NotFound();
    }

    var users = await userRepository.GetUsers();
    var visitingUser = users.SingleOrDefault(u => u.UserId == request.VisitingUserId);

    if (visitingUser == null)
    {
        return this.BadRequest();
    }

    if (!string.Equals(existingGuest.Name, request.Name, StringComparison.OrdinalIgnoreCase) &&
        existingGuests.Any(g =>
            string.Equals(g.Name, request.Name, StringComparison.OrdinalIgnoreCase)))
    {
        return this.Conflict();
    }

    var updatedGuestRequest = new GuestRequest(
        id: existingGuest.Id,
        date: existingGuest.Date,
        name: request.Name,
        visitingUserId: request.VisitingUserId,
        registrationNumber: request.RegistrationNumber,
        status: existingGuest.Status);

    await guestRequestRepository.UpdateGuestRequest(updatedGuestRequest);

    return this.Ok();
}
```

Add the required using at the top of the controller:

```csharp
using NodaTime;
using NodaTime.Text;
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test Parking.Api.UnitTests --filter "Updates_guest_request_successfully"`
Expected: PASS

- [ ] **Step 6: Add remaining PUT validation tests** (not found, invalid user, duplicate name, same name allowed)

- [ ] **Step 7: Run all PUT tests**

Run: `dotnet test Parking.Api.UnitTests --filter "Put"`
Expected: PASS

- [ ] **Step 8: Verify PUT does NOT trigger allocation re-run** (add test that mockTriggerRepository.Verify AddTrigger is never called)

- [ ] **Step 9: Commit**

```bash
git add Parking.Api/Json/GuestRequests/GuestRequestsPutRequest.cs Parking.Api/Controllers/GuestRequestsController.cs Parking.Api.UnitTests/Controllers/GuestRequestsControllerTests.cs
git commit -m "Add PUT /guest-requests/{date}/{id} endpoint"
```

---

## Task 9: GuestRequestsController - DELETE Endpoint (TDD)

**Files:**
- Modify: `Parking.Api/Controllers/GuestRequestsController.cs`
- Modify: `Parking.Api.UnitTests/Controllers/GuestRequestsControllerTests.cs`

- [ ] **Step 1: Write the failing test for successful delete**

```csharp
public static class Delete
{
    [Fact]
    public static async Task Deletes_guest_request_successfully()
    {
        var existingGuest = new GuestRequest(
            "id1", 15.March(2026), "Alice Smith", "user1", null, GuestRequestStatus.Pending);

        var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
        mockGuestRequestRepository
            .Setup(r => r.GetGuestRequests(15.March(2026).ToDateInterval()))
            .ReturnsAsync([existingGuest]);
        mockGuestRequestRepository
            .Setup(r => r.DeleteGuestRequest(15.March(2026), "id1"))
            .Returns(Task.CompletedTask);

        var mockTriggerRepository = new Mock<ITriggerRepository>(MockBehavior.Strict);
        mockTriggerRepository
            .Setup(r => r.AddTrigger())
            .Returns(Task.CompletedTask);

        var controller = CreateController(
            guestRequestRepository: mockGuestRequestRepository.Object,
            triggerRepository: mockTriggerRepository.Object);

        var result = await controller.DeleteAsync("2026-03-15", "id1");

        Assert.IsType<OkResult>(result);
        mockGuestRequestRepository.VerifyAll();
        mockTriggerRepository.VerifyAll();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Parking.Api.UnitTests --filter "Deletes_guest_request_successfully"`
Expected: FAIL

- [ ] **Step 3: Implement DeleteAsync**

```csharp
[HttpDelete("{date}/{id}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> DeleteAsync(string date, string id)
{
    var localDate = LocalDatePattern.Iso.Parse(date);

    if (!localDate.Success)
    {
        return this.BadRequest();
    }

    var existingGuests = await guestRequestRepository.GetGuestRequests(localDate.Value.ToDateInterval());
    var existingGuest = existingGuests.SingleOrDefault(g => g.Id == id);

    if (existingGuest == null)
    {
        return this.NotFound();
    }

    await guestRequestRepository.DeleteGuestRequest(localDate.Value, id);

    await triggerRepository.AddTrigger();

    return this.Ok();
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Parking.Api.UnitTests --filter "Deletes_guest_request_successfully"`
Expected: PASS

- [ ] **Step 5: Add test for not found**

- [ ] **Step 6: Commit**

```bash
git add Parking.Api/Controllers/GuestRequestsController.cs Parking.Api.UnitTests/Controllers/GuestRequestsControllerTests.cs
git commit -m "Add DELETE /guest-requests/{date}/{id} endpoint"
```

---

## Task 10: GuestRequestsController - GET Endpoint (TDD)

**Files:**
- Create: `Parking.Api/Json/GuestRequests/GuestRequestsResponse.cs`
- Create: `Parking.Api/Json/GuestRequests/GuestRequestsData.cs`
- Modify: `Parking.Api/Controllers/GuestRequestsController.cs`
- Modify: `Parking.Api.UnitTests/Controllers/GuestRequestsControllerTests.cs`

- [ ] **Step 1: Create response JSON models**

`GuestRequestsData.cs`:

```csharp
namespace Parking.Api.Json.GuestRequests;

using Model;

public class GuestRequestsData(
    string id,
    string date,
    string name,
    string visitingUserId,
    string visitingUserDisplayName,
    string? registrationNumber,
    GuestRequestStatus status)
{
    public string Id { get; } = id;

    public string Date { get; } = date;

    public string Name { get; } = name;

    public string VisitingUserId { get; } = visitingUserId;

    public string VisitingUserDisplayName { get; } = visitingUserDisplayName;

    public string? RegistrationNumber { get; } = registrationNumber;

    public GuestRequestStatus Status { get; } = status;
}
```

`GuestRequestsResponse.cs`:

```csharp
namespace Parking.Api.Json.GuestRequests;

using System.Collections.Generic;

public class GuestRequestsResponse(IEnumerable<GuestRequestsData> guestRequests)
{
    public IEnumerable<GuestRequestsData> GuestRequests { get; } = guestRequests;
}
```

- [ ] **Step 2: Write test for GET - sorted, with display names**

```csharp
public static class Get
{
    [Fact]
    public static async Task Returns_guest_requests_sorted_by_date_then_name()
    {
        var guestRequests = new[]
        {
            new GuestRequest("id1", 16.March(2026), "Bob", "user1", null, GuestRequestStatus.Allocated),
            new GuestRequest("id2", 15.March(2026), "Charlie", "user1", null, GuestRequestStatus.Pending),
            new GuestRequest("id3", 15.March(2026), "Alice", "user2", "AB12CDE", GuestRequestStatus.Interrupted),
        };

        var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
        mockGuestRequestRepository
            .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
            .ReturnsAsync(guestRequests);

        var users = new[]
        {
            CreateUser.With(userId: "user1", firstName: "John", lastName: "Doe"),
            CreateUser.With(userId: "user2", firstName: "Jane", lastName: "Smith"),
        };

        var controller = CreateController(
            guestRequestRepository: mockGuestRequestRepository.Object,
            userRepository: CreateUserRepository.WithUsers(users));

        var result = await controller.GetAsync();

        var resultValue = ControllerHelpers.GetResultValue<GuestRequestsResponse>(result);

        var items = resultValue.GuestRequests.ToArray();

        Assert.Equal(3, items.Length);
        Assert.Equal("Alice", items[0].Name);
        Assert.Equal("2026-03-15", items[0].Date);
        Assert.Equal("Jane Smith", items[0].VisitingUserDisplayName);
        Assert.Equal("AB12CDE", items[0].RegistrationNumber);
        Assert.Equal(GuestRequestStatus.Interrupted, items[0].Status);

        Assert.Equal("Charlie", items[1].Name);
        Assert.Equal("2026-03-15", items[1].Date);

        Assert.Equal("Bob", items[2].Name);
        Assert.Equal("2026-03-16", items[2].Date);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test Parking.Api.UnitTests --filter "Returns_guest_requests_sorted"`
Expected: FAIL

- [ ] **Step 4: Implement GetAsync**

The GET endpoint needs to calculate the date range: 60 days ago through end of subsequent month. Use `IClock` or `IDateCalculator` for the current date. The `activeDates` from `IDateCalculator` gives us the end boundary (end of next month). For the 60-day lookback, calculate from the current date.

The controller will need an `IClock` dependency for the 60-day lookback calculation. Add it to the constructor.

```csharp
[HttpGet]
[ProducesResponseType(typeof(GuestRequestsResponse), StatusCodes.Status200OK)]
public async Task<IActionResult> GetAsync()
{
    var activeDates = dateCalculator.GetActiveDates();
    var firstDate = activeDates.First().PlusDays(-60);
    var lastDate = activeDates.Last();
    var dateInterval = new DateInterval(firstDate, lastDate);

    var guestRequests = await guestRequestRepository.GetGuestRequests(dateInterval);

    var users = await userRepository.GetUsers();
    var userLookup = users.ToDictionary(u => u.UserId);

    var data = guestRequests
        .Where(g => dateInterval.Contains(g.Date))
        .OrderBy(g => g.Date)
        .ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
        .Select(g => new GuestRequestsData(
            id: g.Id,
            date: LocalDatePattern.Iso.Format(g.Date),
            name: g.Name,
            visitingUserId: g.VisitingUserId,
            visitingUserDisplayName: GetVisitingUserDisplayName(userLookup, g.VisitingUserId),
            registrationNumber: g.RegistrationNumber,
            status: g.Status))
        .ToArray();

    return this.Ok(new GuestRequestsResponse(data));
}

private static string GetVisitingUserDisplayName(
    IReadOnlyDictionary<string, User> userLookup,
    string visitingUserId) =>
    userLookup.TryGetValue(visitingUserId, out var user)
        ? user.DisplayName()
        : "deleted user";
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test Parking.Api.UnitTests --filter "Returns_guest_requests_sorted"`
Expected: PASS

- [ ] **Step 6: Add test for deleted visiting user display name**

```csharp
[Fact]
public static async Task Shows_deleted_user_display_name()
{
    var guestRequests = new[]
    {
        new GuestRequest("id1", 15.March(2026), "Guest", "deleted-user", null, GuestRequestStatus.Pending),
    };

    var mockGuestRequestRepository = new Mock<IGuestRequestRepository>(MockBehavior.Strict);
    mockGuestRequestRepository
        .Setup(r => r.GetGuestRequests(It.IsAny<DateInterval>()))
        .ReturnsAsync(guestRequests);

    var controller = CreateController(
        guestRequestRepository: mockGuestRequestRepository.Object,
        userRepository: CreateUserRepository.WithUsers([]));

    var result = await controller.GetAsync();

    var resultValue = ControllerHelpers.GetResultValue<GuestRequestsResponse>(result);

    Assert.Equal("deleted user", resultValue.GuestRequests.Single().VisitingUserDisplayName);
}
```

- [ ] **Step 7: Add tests for 60-day boundary** (exactly 60 days included, 61 days excluded)

- [ ] **Step 8: Add test for empty list**

- [ ] **Step 9: Commit**

```bash
git add Parking.Api/Json/GuestRequests/ Parking.Api/Controllers/GuestRequestsController.cs Parking.Api.UnitTests/Controllers/GuestRequestsControllerTests.cs
git commit -m "Add GET /guest-requests endpoint"
```

---

## Task 11: AllocationCreator - Guest Request Integration (TDD)

**Files:**
- Create: `Parking.Business/AllocationResult.cs`
- Modify: `Parking.Business/AllocationCreator.cs:10-19` (interface) and `34-77` (implementation)
- Create: `Parking.Business.UnitTests/AllocationCreatorGuestTests.cs`
- Modify: `Parking.Business.UnitTests/AllocationCreatorTests.cs` (update for new signature)

- [ ] **Step 1: Create AllocationResult**

```csharp
namespace Parking.Business;

using System.Collections.Generic;
using Model;

public class AllocationResult(
    IReadOnlyCollection<Request> allocatedRequests,
    IReadOnlyCollection<GuestRequest> updatedGuestRequests)
{
    public IReadOnlyCollection<Request> AllocatedRequests { get; } = allocatedRequests;

    public IReadOnlyCollection<GuestRequest> UpdatedGuestRequests { get; } = updatedGuestRequests;
}
```

- [ ] **Step 2: Write failing test - guests allocated before regular requests**

Create `Parking.Business.UnitTests/AllocationCreatorGuestTests.cs`:

```csharp
namespace Parking.Business.UnitTests;

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Model;
using Moq;
using NodaTime;
using NodaTime.Testing.Extensions;
using Xunit;

public static class AllocationCreatorGuestTests
{
    private static readonly LocalDate AllocationDate = 28.November(2020);
    private static readonly List<User> Users = [];
    private static readonly List<Reservation> Reservations = [];
    private static readonly Configuration Configuration =
        new(nearbyDistance: 1, shortLeadTimeSpaces: 0, totalSpaces: 2);

    [Fact]
    public static void Allocates_guest_requests_before_regular_requests()
    {
        var guestRequests = new[]
        {
            new GuestRequest("g1", AllocationDate, "Guest1", "user1", null, GuestRequestStatus.Pending)
        };

        var regularRequests = new[]
        {
            new Request("user2", AllocationDate, RequestStatus.Interrupted)
        };

        var sortedRequests = new[] { regularRequests[0] };

        var result = CreateAllocationCreator(regularRequests, sortedRequests)
            .Create(AllocationDate, regularRequests, Reservations, Users, Configuration, LeadTimeType.Short, guestRequests);

        Assert.Single(result.AllocatedRequests);
        Assert.Equal("user2", result.AllocatedRequests.Single().UserId);

        Assert.Single(result.UpdatedGuestRequests);
        Assert.Equal(GuestRequestStatus.Allocated, result.UpdatedGuestRequests.Single().Status);
    }

    private static AllocationCreator CreateAllocationCreator(
        IReadOnlyCollection<Request> existingRequests,
        IReadOnlyCollection<Request> sortedRequests)
    {
        var mockRequestSorter = new Mock<IRequestSorter>(MockBehavior.Strict);
        mockRequestSorter
            .Setup(r => r.Sort(
                AllocationDate,
                It.IsAny<IReadOnlyCollection<Request>>(),
                Reservations,
                Users,
                Configuration.NearbyDistance))
            .Returns(sortedRequests);

        return new AllocationCreator(Mock.Of<ILogger<AllocationCreator>>(), mockRequestSorter.Object);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test Parking.Business.UnitTests --filter "AllocationCreatorGuestTests"`
Expected: FAIL - `Create` overload with guest requests does not exist

- [ ] **Step 4: Update IAllocationCreator interface and AllocationCreator**

Update the interface to replace the old method signature with the new one that includes guest requests:

```csharp
public interface IAllocationCreator
{
    AllocationResult Create(
        LocalDate date,
        IReadOnlyCollection<Request> requests,
        IReadOnlyCollection<Reservation> reservations,
        IReadOnlyCollection<User> users,
        Configuration configuration,
        LeadTimeType leadTimeType,
        IReadOnlyCollection<GuestRequest> guestRequests);
}
```

The old overload is removed entirely. All callers must pass guest requests (use an empty collection when there are none).

Implement the new overload in `AllocationCreator`:

```csharp
public AllocationResult Create(
    LocalDate date,
    IReadOnlyCollection<Request> requests,
    IReadOnlyCollection<Reservation> reservations,
    IReadOnlyCollection<User> users,
    Configuration configuration,
    LeadTimeType leadTimeType,
    IReadOnlyCollection<GuestRequest> guestRequests)
{
    var spacesToReserve = leadTimeType == LeadTimeType.Short ? 0 : configuration.ShortLeadTimeSpaces;
    var allocatableSpaces = configuration.TotalSpaces - spacesToReserve;

    var alreadyAllocatedRegular = requests.Count(r => r.Date == date && r.Status == RequestStatus.Allocated);
    var alreadyAllocatedGuests = guestRequests.Count(g => g.Date == date && g.Status == GuestRequestStatus.Allocated);
    var alreadyAllocatedSpaces = alreadyAllocatedRegular + alreadyAllocatedGuests;

    var freeSpaces = allocatableSpaces - alreadyAllocatedSpaces;

    // Allocate pending/interrupted guest requests first
    var pendingGuests = guestRequests
        .Where(g => g.Date == date && g.Status is GuestRequestStatus.Pending or GuestRequestStatus.Interrupted)
        .ToArray();

    var guestsToAllocate = Math.Min(pendingGuests.Length, Math.Max(0, freeSpaces));

    var updatedGuestRequests = pendingGuests
        .Select((g, i) => new GuestRequest(
            g.Id, g.Date, g.Name, g.VisitingUserId, g.RegistrationNumber,
            i < guestsToAllocate ? GuestRequestStatus.Allocated : GuestRequestStatus.Interrupted))
        .ToArray();

    freeSpaces = Math.Max(0, freeSpaces - guestsToAllocate);

    if (freeSpaces <= 0)
    {
        return new AllocationResult(new List<Request>(), updatedGuestRequests);
    }

    var sortedRequests = this.requestSorter
        .Sort(date, requests, reservations, users, configuration.NearbyDistance)
        .ToArray();

    var allocatedRequests = sortedRequests
        .Take(Math.Min(freeSpaces, sortedRequests.Length))
        .Select(r => new Request(r.UserId, r.Date, RequestStatus.Allocated))
        .ToArray();

    return new AllocationResult(allocatedRequests, updatedGuestRequests);
}
```

- [ ] **Step 5: Update existing AllocationCreatorTests** to use the new signature (pass empty guest request collections, use `AllocationResult.AllocatedRequests` instead of the direct return value)

- [ ] **Step 6: Run all tests to verify they pass**

Run: `dotnet test Parking.Business.UnitTests --filter "AllocationCreator"`
Expected: PASS

- [ ] **Step 7: Add more guest allocation tests**

Add tests for:
- More guests than spaces - only allocates up to available
- Zero guests - returns empty updated guest list
- AlreadyAllocatedSpaces includes previously-allocated guests
- Mix of allocated and interrupted guests when partially full
- Guests mixed with reservations and regular requests
- Delete guest triggers reallocation - previously-unallocated request gets allocated (test at RequestUpdater level)

- [ ] **Step 8: Commit**

```bash
git add Parking.Business/AllocationResult.cs Parking.Business/AllocationCreator.cs Parking.Business.UnitTests/AllocationCreatorGuestTests.cs Parking.Business.UnitTests/AllocationCreatorTests.cs
git commit -m "Add guest request support to AllocationCreator"
```

---

## Task 12: RequestUpdater - Guest Request Integration (TDD)

**Files:**
- Modify: `Parking.Business/RequestUpdater.cs`
- Modify: `Parking.Business.UnitTests/RequestUpdaterTests.cs` (or create new test file)

**Note on guest status reset:** Unlike regular requests (where Pending is reset to Interrupted before allocation), guest requests do NOT need a separate reset step. The `AllocationCreator` already processes guests with `Pending` or `Interrupted` status and sets them to `Allocated` or `Interrupted` as appropriate. Previously-Allocated guests are left alone (matching regular request behaviour).

- [ ] **Step 1: Write failing test - fetches and passes guest requests to AllocationCreator**

The test should verify that `RequestUpdater.Update()`:
1. Fetches guest requests from the repository
2. Passes them to `AllocationCreator.Create()` (the new overload)
3. Saves updated guest request statuses

- [ ] **Step 2: Run test to verify it fails**

- [ ] **Step 3: Update RequestUpdater**

Add `IGuestRequestRepository` dependency to constructor. In `Update()`:
- Fetch guest requests for the cache interval
- Use the new `Create` overload that accepts guest requests
- Collect updated guest requests from `AllocationResult`
- Save updated guest requests via `guestRequestRepository.SaveGuestRequests()`

```csharp
public RequestUpdater(
    IAllocationCreator allocationCreator,
    IConfigurationRepository configurationRepository,
    IDateCalculator dateCalculator,
    IGuestRequestRepository guestRequestRepository,
    IRequestRepository requestRepository,
    IReservationRepository reservationRepository,
    IUserRepository userRepository)
```

In the `Update()` method, after fetching requests/reservations:

```csharp
var guestRequests = await this.guestRequestRepository.GetGuestRequests(cacheInterval);
var guestRequestsCache = guestRequests.ToList();
var newGuestRequests = new List<GuestRequest>();
```

Replace the allocation loop to use the new overload:

```csharp
foreach (var allocationDate in shortLeadTimeAllocationDates)
{
    var allocationResult = this.allocationCreator.Create(
        allocationDate, requestsCache, reservations, users, configuration, LeadTimeType.Short, guestRequestsCache);

    UpdateRequests(newRequests, allocationResult.AllocatedRequests);
    UpdateRequests(requestsCache, allocationResult.AllocatedRequests);
    newGuestRequests.AddRange(allocationResult.UpdatedGuestRequests);
    UpdateGuestRequests(guestRequestsCache, allocationResult.UpdatedGuestRequests);
}
// Same for longLeadTimeAllocationDates

await this.guestRequestRepository.SaveGuestRequests(newGuestRequests);
```

Add `UpdateGuestRequests` helper:

```csharp
private static void UpdateGuestRequests(
    ICollection<GuestRequest> existingGuests,
    IEnumerable<GuestRequest> updatedGuests)
{
    foreach (var updated in updatedGuests)
    {
        var previous = existingGuests.SingleOrDefault(g => g.Id == updated.Id);

        if (previous != null)
        {
            existingGuests.Remove(previous);
        }

        existingGuests.Add(updated);
    }
}
```

- [ ] **Step 4: Update existing RequestUpdaterTests** for the new constructor signature (add `Mock.Of<IGuestRequestRepository>()` or a proper mock)

- [ ] **Step 5: Run all tests**

Run: `dotnet test Parking.Business.UnitTests --filter "RequestUpdater"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add Parking.Business/RequestUpdater.cs Parking.Business.UnitTests/RequestUpdaterTests.cs
git commit -m "Add guest request support to RequestUpdater"
```

---

## Task 13: DailyDetails - Show Guest Requests (TDD)

**Files:**
- Modify: `Parking.Api/Controllers/DailyDetailsController.cs`
- Modify: `Parking.Api.UnitTests/Controllers/DailyDetailsControllerTests.cs`

- [ ] **Step 1: Write failing test - allocated guest appears in allocated list**

```csharp
[Fact]
public static async Task Shows_allocated_guest_in_allocated_users()
{
    var activeDates = new[] { 12.July(2021) };

    var dateCalculator = CreateDateCalculator.WithActiveDates(activeDates);

    var users = new[]
    {
        CreateUser.With(userId: "user1", firstName: "John", lastName: "Doe"),
    };

    var requests = new[]
    {
        new Request("user1", 12.July(2021), RequestStatus.Allocated),
    };

    var guestRequests = new[]
    {
        new GuestRequest("g1", 12.July(2021), "Alice Smith", "user1", null, GuestRequestStatus.Allocated),
    };

    var requestRepository = new RequestRepositoryBuilder()
        .WithGetRequests(activeDates.ToDateInterval(), requests)
        .Build();

    var guestRequestRepository = new GuestRequestRepositoryBuilder()
        .WithGetGuestRequests(activeDates.ToDateInterval(), guestRequests)
        .Build();

    var controller = new DailyDetailsController(
        dateCalculator,
        guestRequestRepository,
        requestRepository,
        Mock.Of<ITriggerRepository>(),
        CreateUserRepository.WithUsers(users))
    {
        ControllerContext = CreateControllerContext.WithUsername("user1")
    };

    var result = await controller.GetAsync();

    var resultValue = GetResultValue<DailyDetailsResponse>(result);
    var data = GetDailyData(resultValue.Details, 12.July(2021));

    Assert.Equal(2, data.AllocatedUsers.Count());
    Assert.Contains(data.AllocatedUsers, u => u.Name == "Alice Smith (visiting John Doe)");
}
```

- [ ] **Step 2: Run test to verify it fails**

- [ ] **Step 3: Update DailyDetailsController**

Add `IGuestRequestRepository` to constructor. In `GetAsync()`, fetch guest requests and pass to `CreateDailyData`. Modify `CreateDailyData` to merge guest requests into the appropriate lists:

```csharp
private static Day<DailyDetailsData> CreateDailyData(
    LocalDate localDate,
    string currentUserId,
    IReadOnlyCollection<Request> requests,
    IReadOnlyCollection<GuestRequest> guestRequests,
    IReadOnlyCollection<User> users)
{
    var filteredRequests = requests.Where(r => r.Date == localDate).ToArray();
    var filteredGuests = guestRequests.Where(g => g.Date == localDate).ToArray();

    var userLookup = users.ToDictionary(u => u.UserId);

    var allocatedUsers = CreateDailyDetailUsers(currentUserId, filteredRequests.Where(r => r.Status == RequestStatus.Allocated), users)
        .Concat(filteredGuests
            .Where(g => g.Status == GuestRequestStatus.Allocated)
            .Select(g => new DailyDetailsUser(
                name: FormatGuestName(g, userLookup),
                isHighlighted: false)));

    // Similar for interrupted and pending...
}

private static string FormatGuestName(
    GuestRequest guest,
    IReadOnlyDictionary<string, User> userLookup) =>
    userLookup.TryGetValue(guest.VisitingUserId, out var user)
        ? $"{guest.Name} (visiting {user.DisplayName()})"
        : $"{guest.Name} (visiting deleted user)";
```

- [ ] **Step 4: Run test to verify it passes**

- [ ] **Step 5: Add tests for pending and interrupted guests, deleted visiting user, unallocated guest not in allocated list, mix of guest and regular users**

- [ ] **Step 6: Update existing DailyDetails tests** for new constructor signature (add `Mock.Of<IGuestRequestRepository>()` or empty guest request repository)

- [ ] **Step 7: Run all DailyDetails tests**

Run: `dotnet test Parking.Api.UnitTests --filter "DailyDetailsControllerTests"`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add Parking.Api/Controllers/DailyDetailsController.cs Parking.Api.UnitTests/Controllers/DailyDetailsControllerTests.cs
git commit -m "Show guest requests in daily details"
```

---

## Task 14: Registration Numbers - Include Guest Requests (TDD)

**Files:**
- Modify: `Parking.Api/Controllers/RegistrationNumbersController.cs`
- Modify: `Parking.Api.UnitTests/Controllers/RegistrationNumbersControllerTests.cs`

- [ ] **Step 1: Write failing test - guest registration number is searchable**

```csharp
[Fact]
public static async Task Returns_guest_registration_numbers()
{
    var users = new[]
    {
        CreateUser.With(userId: "user1", firstName: "John", lastName: "Doe", registrationNumber: "AB12CDE"),
    };

    var guestRequests = new[]
    {
        new GuestRequest("g1", 15.March(2026), "Alice Smith", "user1", "XY34FGH", GuestRequestStatus.Pending),
    };

    var guestRequestRepository = new GuestRequestRepositoryBuilder()
        .WithGetGuestRequests(It.IsAny<DateInterval>(), guestRequests)
        .Build();

    var controller = new RegistrationNumbersController(
        guestRequestRepository,
        CreateUserRepository.WithUsers(users));

    var result = await controller.GetAsync("XY34FGH");

    var resultValue = GetResultValue<RegistrationNumbersResponse>(result);

    Assert.Single(resultValue.RegistrationNumbers);
    Assert.Equal("XY34FGH", resultValue.RegistrationNumbers.Single().RegistrationNumber);
    Assert.Equal("Alice Smith (visiting John Doe)", resultValue.RegistrationNumbers.Single().Name);
}
```

Note: The `GuestRequestRepositoryBuilder.WithGetGuestRequests` might need to accept `It.IsAny<DateInterval>()` since the registration numbers endpoint doesn't have a specific date range. The controller will need to query all guest requests (use a wide date range or a dedicated method).

- [ ] **Step 2: Run test to verify it fails**

- [ ] **Step 3: Update RegistrationNumbersController**

Add `IGuestRequestRepository` to constructor. Fetch all guest requests (wide date range) and include their registration numbers in the search:

```csharp
public class RegistrationNumbersController(
    IDateCalculator dateCalculator,
    IGuestRequestRepository guestRequestRepository,
    IUserRepository userRepository) : ControllerBase
{
    [HttpGet("{searchString}")]
    public async Task<IActionResult> GetAsync(string searchString)
    {
        var users = await userRepository.GetUsers();
        var userLookup = users.ToDictionary(u => u.UserId);

        var userData = users
            .Select(CreateRegistrationNumbersData)
            .SelectMany(d => d);

        var activeDates = dateCalculator.GetActiveDates();
        var guestDateInterval = new DateInterval(activeDates.First().PlusDays(-60), activeDates.Last());

        var guestRequests = await guestRequestRepository.GetGuestRequests(guestDateInterval);

        var guestData = guestRequests
            .Where(g => !string.IsNullOrEmpty(g.RegistrationNumber))
            .Select(g => new RegistrationNumbersData(
                FormatRegistrationNumber(g.RegistrationNumber!),
                FormatGuestName(g, userLookup)));

        var data = userData.Concat(guestData)
            .Where(d =>
                !string.IsNullOrEmpty(d.RegistrationNumber) &&
                NormalizeRegistrationNumber(d.RegistrationNumber) == NormalizeRegistrationNumber(searchString))
            .OrderBy(d => d.RegistrationNumber)
            .ToArray();

        return this.Ok(new RegistrationNumbersResponse(data));
    }
}
```

Use the same date range as the GET endpoint: 60 days back from the first active date through the last active date. This requires adding `IDateCalculator` as a dependency to the controller. This bounded range covers all guest requests that could exist (since creation is limited to active dates).

- [ ] **Step 4: Run test to verify it passes**

- [ ] **Step 5: Add tests** for guest with no registration number excluded, normalization on guest reg numbers, deleted visiting user name format

- [ ] **Step 6: Update existing RegistrationNumbers tests** for new constructor signature

- [ ] **Step 7: Run all tests**

Run: `dotnet test Parking.Api.UnitTests --filter "RegistrationNumbersControllerTests"`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add Parking.Api/Controllers/RegistrationNumbersController.cs Parking.Api.UnitTests/Controllers/RegistrationNumbersControllerTests.cs
git commit -m "Include guest registration numbers in search"
```

---

## Task 15: Integration Tests for GuestRequestRepository

**Files:**
- Create: `Parking.Data.UnitTests/GuestRequestRepositoryTests.cs` (or in integration test project)

Check existing integration test patterns in `Parking.Api.IntegrationTests/` or `Parking.Data.UnitTests/` to determine the right location and pattern for DynamoDB repository integration tests.

- [ ] **Step 1: Write integration tests**

Tests for:
- Save and retrieve guest request
- Update guest request
- Delete guest request
- Query by date range spanning multiple months
- Batch save (status updates)

- [ ] **Step 2: Run integration tests**

Run: `dotnet test` (relevant test project)
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add <test files>
git commit -m "Add GuestRequestRepository integration tests"
```

---

## Task 16: Full Test Suite and Cleanup

- [ ] **Step 1: Run all tests**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 2: Fix any compilation errors or test failures**

- [ ] **Step 3: Review for consistency** - naming conventions, JSON serialization, error handling

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "Guest requests feature: cleanup and final fixes"
```
