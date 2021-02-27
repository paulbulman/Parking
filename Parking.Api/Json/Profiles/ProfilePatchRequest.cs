﻿namespace Parking.Api.Json.Profiles
{
    public class ProfilePatchRequest
    {
        public ProfilePatchRequest(string registrationNumber, string alternativeRegistrationNumber)
        {
            this.RegistrationNumber = registrationNumber;
            this.AlternativeRegistrationNumber = alternativeRegistrationNumber;
        }
        
        public string RegistrationNumber { get; }
        
        public string AlternativeRegistrationNumber { get; }
    }
}