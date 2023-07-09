using System;
using System.Collections;
using System.Collections.Generic;
using MWM.PrototypeTemplate.UI;
using UnityEngine;

public interface IReward
{
   void SetupRewardStartValue();
   RewardData GetData();
   void GainReward();
   void GainBonus();
   void Multiply(float multiplier);
   Sprite GetIcon();
   Sprite GetIconLarge();
   bool IsChest();
   int GetStartValue();
   int GetQuantity();
   int GetBonus();
   float GetMultiplier();
   RewardType GetRewardType();
   string ToString();
}
