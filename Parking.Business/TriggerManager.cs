﻿namespace Parking.Business
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Data;

    public class TriggerManager
    {
        private readonly ITriggerRepository triggerRepository;
        
        private readonly List<string> triggers = new List<string>();
    
        public TriggerManager(ITriggerRepository triggerRepository) =>
            this.triggerRepository = triggerRepository;

        public async Task<bool> ShouldRun()
        {
            if (this.triggers.Any())
            {
                throw new InvalidOperationException("Previous run not completed");
            }

            this.triggers.AddRange(await this.triggerRepository.GetKeys());

            return this.triggers.Any();
        }

        public async Task MarkComplete()
        {
            await this.triggerRepository.DeleteKeys(this.triggers.ToArray());

            this.triggers.Clear();
        }
    }
}