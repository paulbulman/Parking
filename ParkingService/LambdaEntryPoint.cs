namespace ParkingService
{
    using System;
    using Amazon.Lambda.Core;

    public class LambdaEntryPoint
    {
        public void RunTasks(ILambdaContext context)
        {
            Console.WriteLine($"Service run at {System.DateTime.Now}");
        }
    }
}
