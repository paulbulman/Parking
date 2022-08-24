namespace Parking.Data.Aws
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.CognitoIdentityProvider;
    using Amazon.CognitoIdentityProvider.Model;

    public interface IIdentityProvider
    {
        Task<string> CreateUser(string emailAddress, string firstName, string lastName);
        
        Task<IReadOnlyCollection<string>> GetUserIdsInGroup(string groupName);
        
        Task UpdateUser(string userId, string firstName, string lastName);

        Task DeleteUser(string userId);
    }

    public class IdentityProvider : IIdentityProvider
    {
        private readonly IAmazonCognitoIdentityProvider cognitoIdentityProvider;

        public IdentityProvider(IAmazonCognitoIdentityProvider cognitoIdentityProvider) =>
            this.cognitoIdentityProvider = cognitoIdentityProvider;

        private static string UserPoolId => Helpers.GetRequiredEnvironmentVariable("USER_POOL_ID");

        public async Task<string> CreateUser(string emailAddress, string firstName, string lastName)
        {
            var result = await this.cognitoIdentityProvider.AdminCreateUserAsync(new AdminCreateUserRequest
            {
                Username = emailAddress,
                UserPoolId = UserPoolId,
                UserAttributes = new List<AttributeType>
                {
                    new AttributeType {Name = "given_name", Value = firstName},
                    new AttributeType {Name = "family_name", Value = lastName},
                    new AttributeType {Name = "email", Value = emailAddress},
                    new AttributeType {Name = "email_verified", Value = "true"},
                }
            });

            return result.User.Username;
        }

        public async Task<IReadOnlyCollection<string>> GetUserIdsInGroup(string groupName)
        {
            var request = new ListUsersInGroupRequest
            {
                GroupName = groupName,
                UserPoolId = UserPoolId
            };

            var response = await this.cognitoIdentityProvider.ListUsersInGroupAsync(request);

            return response
                .Users
                .Select(u => u.Username)
                .ToArray();
        }

        public async Task UpdateUser(string userId, string firstName, string lastName) =>
            await this.cognitoIdentityProvider.AdminUpdateUserAttributesAsync(new AdminUpdateUserAttributesRequest
            {
                Username = userId,
                UserPoolId = UserPoolId,
                UserAttributes = new List<AttributeType>
                {
                    new AttributeType {Name = "given_name", Value = firstName},
                    new AttributeType {Name = "family_name", Value = lastName},
                }
            });

        public async Task DeleteUser(string userId) =>
            await this.cognitoIdentityProvider.AdminDeleteUserAsync(new AdminDeleteUserRequest
            {
                Username = userId,
                UserPoolId = UserPoolId
            });
    }
}