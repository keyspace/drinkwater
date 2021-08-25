﻿using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace DrinkWater
{
	public struct CharacterStats
	{
		public IMyCharacter character;
		public MyEntityStat water;
		public MyEntityStat food;
		public MyEntityStat sleep;
	}

	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class Session : MySessionComponentBase
	{
		private static int skippedTicks = 0;
		private static List<IMyPlayer> players = new List<IMyPlayer>();
		private static List<CharacterStats> charactersStats = new List<CharacterStats>();
		private bool isServer;

		private const float WATER_USAGE = 0.03f;
		private const float FOOD_USAGE = 0.015f;
		private const float SLEEP_USAGE = 0.01f;
		private const float WATER_DAMAGE = 3f;
		private const float FOOD_DAMAGE = 1.5f;
		private const float SLEEP_DAMAGE = 1f;
		private const float SLEEP_GAIN_SITTING = 1f;
		private const float SLEEP_GAIN_SLEEPING = 34f;

		public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
		{
			isServer = MyAPIGateway.Multiplayer.IsServer;
			if (!isServer)
			{
				return;
			}
			UpdateAfterSimulation100();
		}

		public override void UpdateAfterSimulation()
		{
			if (!isServer)
			{
				return;
			}
			if (skippedTicks++ <= 100)
			{
				if (skippedTicks % 10 == 0)
				{
					UpdateAfterSimulation10();
				}
			}
			else
			{
				skippedTicks = 0;
				UpdateAfterSimulation100();
			}
		}

		public void UpdateAfterSimulation10()
		{
			foreach (CharacterStats characterStats in charactersStats)
			{
				if (characterStats.character.EnabledHelmet)
				{
					if (characterStats.water.HasAnyEffect())
					{
						characterStats.character.SwitchHelmet();
						MyAPIGateway.Utilities.ShowNotification("Helmet opened to drink!", 3000);
					}
					else if (characterStats.food.HasAnyEffect())
					{
						characterStats.character.SwitchHelmet();
						MyAPIGateway.Utilities.ShowNotification("Helmet opened to eat!", 3000);
					}
				}
			}
		}

		public void UpdateAfterSimulation100()
		{
			players.Clear();
			charactersStats.Clear();
			MyAPIGateway.Players.GetPlayers(players);

			foreach (IMyPlayer player in players)
			{
				if (player.IsBot == true)
				{
					continue;
				}
				MyEntityStatComponent statComp = player.Character?.Components?.Get<MyEntityStatComponent>();
				if (statComp == null)
				{
					continue;
				}

				MyEntityStat water;
				MyEntityStat food;
				MyEntityStat sleep;
				statComp.TryGetStat(MyStringHash.GetOrCompute("Water"), out water);
				statComp.TryGetStat(MyStringHash.GetOrCompute("Food"), out food);
				statComp.TryGetStat(MyStringHash.GetOrCompute("Sleep"), out sleep);

				charactersStats.Add(new CharacterStats
				{
					character = player.Character,
					water = water,
					food = food,
					sleep = sleep
				});

				if (water.Value <= 0)
				{
					player.Character.DoDamage(WATER_DAMAGE, MyDamageType.Unknown, true);
				}

				if (food.Value <= 0)
				{
					player.Character.DoDamage(FOOD_DAMAGE, MyDamageType.Unknown, true);
				}

				if (sleep.Value <= 0)
				{
					player.Character.DoDamage(SLEEP_DAMAGE, MyDamageType.Unknown, true);
				}

				bool inCryoOrBed = false;

				if (player.Character.CurrentMovementState == MyCharacterMovementEnum.Sitting &&
					(player.Controller.ControlledEntity as IMyShipController) != null &&
					!(player.Controller.ControlledEntity as IMyShipController).CanControlShip)
				{
					//Sitting, but not working
					inCryoOrBed = player.Controller.ControlledEntity.ToString().StartsWith("MyCryoChamber");
					float sleepGain = inCryoOrBed ? SLEEP_GAIN_SLEEPING : SLEEP_GAIN_SITTING;
					sleep.Increase(sleepGain, null);
				}
				else
				{
					sleep.Decrease(SLEEP_USAGE, null);
				}

				if (!inCryoOrBed)
				{
					food.Decrease(FOOD_USAGE, null);
					water.Decrease(WATER_USAGE, null);
				}
			}
		}
	}
}
