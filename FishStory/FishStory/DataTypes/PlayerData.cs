﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishStory.DataTypes
{
    public class PlayerData
    {
        public Dictionary<string, int> ItemInventory { get; set; } =
            new Dictionary<string, int>();

        

        public void AwardItem(string itemKey)
        {
            if(ItemInventory.ContainsKey(itemKey) == false)
            {
                ItemInventory[itemKey] = 1;
            }
            else
            {
                ItemInventory[itemKey]++;
            }
        }

        public bool Has(string itemKey, int desiredCount = 1)
        {
            var inventoryCount = 0;

            if(ItemInventory.ContainsKey(itemKey))
            {
                inventoryCount = ItemInventory[itemKey];
            }

            return inventoryCount >= desiredCount;
        }
        
    }
}