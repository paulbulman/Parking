namespace Parking.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Aws;
    using Business;
    using Business.Data;
    using Model;

    public class UserRepository : IUserRepository
    {
        private readonly IDatabaseProvider databaseProvider;
        
        private readonly IIdentityProvider identityProvider;

        public UserRepository(IDatabaseProvider databaseProvider,  IIdentityProvider identityProvider)
        {
            this.databaseProvider = databaseProvider;
            this.identityProvider = identityProvider;
        }

        public async Task<User> CreateUser(User user)
        {
            var userId = await this.identityProvider.CreateUser(user.EmailAddress, user.FirstName, user.LastName);

            var newUser = new User(
                userId: userId,
                alternativeRegistrationNumber: user.AlternativeRegistrationNumber,
                commuteDistance: user.CommuteDistance,
                emailAddress: user.EmailAddress,
                firstName: user.FirstName,
                lastName: user.LastName,
                registrationNumber: user.RegistrationNumber,
                requestReminderEnabled: user.RequestReminderEnabled,
                reservationReminderEnabled: user.ReservationReminderEnabled);
                
            await this.databaseProvider.SaveItem(CreateRawItem(newUser));

            return newUser;
        }

        public async Task<bool> UserExists(string userId)
        {
            var queryResult = await this.databaseProvider.GetUser(userId);
            
            return queryResult != null;
        }

        public async Task<User?> GetUser(string userId)
        {
            var queryResult = await this.databaseProvider.GetUser(userId);

            return queryResult != null ? CreateUser(queryResult) : null;
        }

        public async Task<IReadOnlyCollection<User>> GetUsers()
        {
            var queryResult = await this.databaseProvider.GetUsers();

            return queryResult
                .Select(CreateUser)
                .ToArray();
        }

        public async Task SaveUser(User user)
        {
            await this.identityProvider.UpdateUser(user.UserId, user.FirstName, user.LastName);

            await this.databaseProvider.SaveItem(CreateRawItem(user));
        }

        public async Task<IReadOnlyCollection<User>> GetTeamLeaderUsers()
        {
            var allUsers = await this.GetUsers();

            var teamLeaderUserIds = await this.identityProvider.GetUserIdsInGroup(Constants.TeamLeaderGroupName);

            return allUsers
                .Where(u => teamLeaderUserIds.Contains(u.UserId))
                .ToArray();
        }

        private static User CreateUser(RawItem rawItem)
        {
            if (rawItem.EmailAddress == null)
            {
                throw new InvalidOperationException("Email address cannot be null.");
            }

            if (rawItem.FirstName == null)
            {
                throw new InvalidOperationException("First name cannot be null.");
            }

            if (rawItem.LastName == null)
            {
                throw new InvalidOperationException("Last name cannot be null.");
            }

            return new User(
                userId: rawItem.PrimaryKey.Split('#')[1],
                alternativeRegistrationNumber: rawItem.AlternativeRegistrationNumber,
                commuteDistance: rawItem.CommuteDistance,
                emailAddress: rawItem.EmailAddress,
                firstName: rawItem.FirstName,
                lastName: rawItem.LastName,
                registrationNumber: rawItem.RegistrationNumber,
                requestReminderEnabled: rawItem.RequestReminderEnabled ?? true,
                reservationReminderEnabled: rawItem.ReservationReminderEnabled ?? true);
        }

        private static RawItem CreateRawItem(User user) =>
            RawItem.CreateUser(
                primaryKey: $"USER#{user.UserId}",
                sortKey: "PROFILE",
                alternativeRegistrationNumber: user.AlternativeRegistrationNumber,
                commuteDistance: user.CommuteDistance,
                emailAddress: user.EmailAddress,
                firstName: user.FirstName,
                lastName: user.LastName,
                registrationNumber: user.RegistrationNumber,
                requestReminderEnabled: user.RequestReminderEnabled,
                reservationReminderEnabled: user.ReservationReminderEnabled);
    }
}