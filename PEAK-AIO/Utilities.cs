using BepInEx.Logging;
using DearImGuiInjection.BepInEx;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zorro.Core.Serizalization;

public static class Utilities
{
    private static ManualLogSource Logger => ConfigManager.Logger;

    public static void GetPlayer()
    {
        if (Globals.playerObj == null)
            Globals.playerObj = Player.localPlayer;
    }

    public static void UpdateItems()
    {
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            Globals.items.Clear();
            Globals.itemNames.Clear();
            for (int i = 0; i < 3; i++) Globals.selectedItems[i] = -1;

            UnityEngine.Object[] allItems = Resources.FindObjectsOfTypeAll(typeof(Item));

            foreach (var obj in allItems)
            {
                var item = obj as Item;
                if (item != null && item.gameObject.scene.handle == 0 && string.IsNullOrEmpty(item.gameObject.scene.name))
                {
                    Globals.items.Add(item);
                    Globals.itemNames.Add(item.GetName());
                }
            }
        });
    }

    public static void AssignInventoryItem(int slot, int itemIndex)
    {
        GetPlayer();

        if (Globals.playerObj == null)
        {
            Logger.LogError("[PEAK AIO] Player is null during inventory operation");
            return;
        }

        if (Globals.playerObj != null &&
            Globals.playerObj.itemSlots != null &&
            Globals.playerObj.itemSlots.Length > slot &&
            itemIndex >= 0 && itemIndex < Globals.items.Count)
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                var slotData = Globals.playerObj.itemSlots[slot];
                slotData.prefab = Globals.items[itemIndex];
                slotData.data = new ItemInstanceData(Guid.NewGuid());
                ItemInstanceDataHandler.AddInstanceData(slotData.data);

                byte[] syncData = IBinarySerializable.ToManagedArray<InventorySyncData>(
                    new InventorySyncData(
                        Globals.playerObj.itemSlots,
                        Globals.playerObj.backpackSlot,
                        Globals.playerObj.tempFullSlot
                    )
                );

                Globals.playerObj.photonView.RPC("SyncInventoryRPC", RpcTarget.Others, new object[] { syncData, true });
            });
            Logger.LogInfo($"[Inventory] Assigned {Globals.itemNames[itemIndex]} to slot {slot}");
        }
    }

    public static void RechargeInventorySlot(int slot, float rechargeValue)
    {
        GetPlayer();

        if (Globals.playerObj == null)
        {
            Logger.LogError("[PEAK AIO] Player is null during inventory operation");
            return;
        }

        if (Globals.playerObj != null &&
            Globals.playerObj.itemSlots != null &&
            Globals.playerObj.itemSlots.Length > slot)
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                var itemSlot = Globals.playerObj.itemSlots[slot];
                if (itemSlot?.data?.data != null)
                {
                    foreach (var kvp in itemSlot.data.data)
                    {
                        if (kvp.Key == DataEntryKey.PetterItemUses)
                        {
                            if (kvp.Value is IntItemData intData)
                            {
                                intData.Value = (int)rechargeValue;
                            }
                        }
                        else if (kvp.Key == DataEntryKey.Fuel)
                        {
                            if (kvp.Value is FloatItemData floatData)
                            {
                                floatData.Value = rechargeValue;
                            }
                        }
                        else if (kvp.Key == DataEntryKey.UseRemainingPercentage)
                        {
                            if (kvp.Value is FloatItemData floatData)
                            {
                                floatData.Value = rechargeValue;
                            }
                        }
                        else if (kvp.Key == DataEntryKey.ItemUses)
                        {
                            if (kvp.Value is OptionableIntItemData intData)
                            {
                                intData.Value = (int)rechargeValue;
                            }
                        }
                    }
                }
            });
            Logger.LogInfo($"[Inventory] Recharged slot {slot} to {rechargeValue}");
        }
    }

    public static void RefreshPlayerList()
    {
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            try
            {
                Globals.allPlayers.Clear();
                Globals.playerNames.Clear();
                Globals.selectedPlayer = -1;

                var characters = Character.AllCharacters;
                if (characters == null || characters.Count == 0)
                    return;

                for (int i = 0; i < characters.Count; i++)
                {
                    try
                    {
                        var character = characters[i];
                        if (character == null) continue;
                        string name = character.characterName ?? "Unknown";
                        Globals.allPlayers.Add(character);
                        Globals.playerNames.Add(name);
                    }
                    catch
                    {
                        continue;
                    }
                }
                Logger.LogInfo($"[PlayerList] Found {Globals.allPlayers.Count} players.");
            }
            catch (Exception ex)
            {
                ConfigManager.Logger.LogError(ex);
            }
        });
    }

    public static void ReviveAllPlayers()
    {
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            try
            {
                var characters = Character.AllCharacters;
                if (characters == null || characters.Count == 0)
                    return;

                for (int i = 0; i < characters.Count; i++)
                {
                    try
                    {
                        var character = characters[i];
                        if (character == null || character.photonView == null) continue;

                        Vector3 revivePos = character.Ghost != null
                            ? character.Ghost.transform.position
                            : character.Head;
                        character.photonView.RPC("RPCA_ReviveAtPosition", RpcTarget.All, new object[] {
                            revivePos + new Vector3(0f, 4f, 0f), false, -1
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"[Lobby] Revive failed for a character: {ex.Message}");
                    }
                }
                Logger.LogInfo("[Lobby] Revive All triggered.");
            }
            catch (Exception ex)
            {
                ConfigManager.Logger.LogError(ex);
            }
        });
    }

    public static void KillAllPlayers()
    {
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            try
            {
                var characters = Character.AllCharacters;
                if (characters == null || characters.Count == 0)
                    return;

                for (int i = 0; i < characters.Count; i++)
                {
                    try
                    {
                        var character = characters[i];
                        if (character == null || character.photonView == null) continue;
                        if (Globals.excludeSelfFromAllActions && character.IsLocal)
                            continue;

                        Vector3 pos = character.transform.position;
                        character.photonView.RPC("RPCA_Die", RpcTarget.All, new object[] { pos });
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"[Lobby] Kill failed for a character: {ex.Message}");
                    }
                }

                Logger.LogInfo($"[Lobby] Kill All triggered. ExcludeSelf: {Globals.excludeSelfFromAllActions}");
            }
            catch (Exception ex)
            {
                ConfigManager.Logger.LogError(ex);
            }
        });
    }

    public static void WarpAllPlayersToMe()
    {
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            try
            {
                if (Character.localCharacter == null)
                    return;

                var characters = Character.AllCharacters;
                if (characters == null || characters.Count == 0)
                    return;

                Vector3 myPos = Character.localCharacter.Head + new Vector3(0f, 4f, 0f);
                for (int i = 0; i < characters.Count; i++)
                {
                    try
                    {
                        var character = characters[i];
                        if (character == null || character.photonView == null) continue;
                        character.photonView.RPC("WarpPlayerRPC", RpcTarget.All, new object[] { myPos, true });
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"[Lobby] Warp failed for a character: {ex.Message}");
                    }
                }
                Logger.LogInfo("[Lobby] Warp All To Me triggered.");
            }
            catch (Exception ex)
            {
                ConfigManager.Logger.LogError(ex);
            }
        });
    }


    public static void ReviveSelectedPlayer()
    {
        if (Globals.selectedPlayer < 0 || Globals.selectedPlayer >= Globals.allPlayers.Count)
            return;

        UnityMainThreadDispatcher.Enqueue(() =>
        {
            try
            {
                var target = Globals.allPlayers[Globals.selectedPlayer];
                if (target == null || target.photonView == null)
                    return;

                Vector3 revivePos = target.Ghost != null ? target.Ghost.transform.position : target.Head;
                target.photonView.RPC("RPCA_ReviveAtPosition", RpcTarget.All, new object[] {
                    revivePos + new Vector3(0f, 4f, 0f), false, -1
                });
                Logger.LogInfo($"[Lobby] Revive requested for player index {Globals.selectedPlayer}");
            }
            catch (Exception ex)
            {
                ConfigManager.Logger.LogError(ex);
            }
        });
    }

    public static void KillSelectedPlayer()
    {
        if (Globals.selectedPlayer < 0 || Globals.selectedPlayer >= Globals.allPlayers.Count)
            return;

        UnityMainThreadDispatcher.Enqueue(() =>
        {
            try
            {
                var target = Globals.allPlayers[Globals.selectedPlayer];
                Vector3 spawnPoint = target.transform.position; // or any desired location
                target.photonView.RPC("RPCA_Die", RpcTarget.All, new object[] { spawnPoint });
                Logger.LogInfo($"[Lobby] Kill requested for player index {Globals.selectedPlayer}");
            }
            catch (Exception ex)
            {
                ConfigManager.Logger.LogError(ex);
            }
        });
    }

    public static void WarpToSelectedPlayer()
    {
        if (Globals.selectedPlayer < 0 || Globals.selectedPlayer >= Globals.allPlayers.Count)
            return;

        UnityMainThreadDispatcher.Enqueue(() =>
        {
            try
            {
                var target = Globals.allPlayers[Globals.selectedPlayer];
                Vector3 targetPos = target.Head + new Vector3(0f, 4f, 0f);
                Character.localCharacter.photonView.RPC("WarpPlayerRPC", RpcTarget.All, new object[] {
                targetPos, true
            });
                Logger.LogInfo($"[Lobby] Warp to requested for player index {Globals.selectedPlayer}");
            }
            catch (Exception ex)
            {
                ConfigManager.Logger.LogError(ex);
            }
        });
    }

    public static void WarpSelectedPlayerToMe()
    {
        if (Globals.selectedPlayer < 0 || Globals.selectedPlayer >= Globals.allPlayers.Count)
            return;

        UnityMainThreadDispatcher.Enqueue(() =>
        {
            try
            {
                var target = Globals.allPlayers[Globals.selectedPlayer];
                Vector3 myHead = Character.localCharacter.Head + new Vector3(0f, 4f, 0f);
                target.photonView.RPC("WarpPlayerRPC", RpcTarget.All, new object[] {
                myHead, true
            });
                Logger.LogInfo($"[Lobby] Warp to me requested for player index {Globals.selectedPlayer}");
            }
            catch (Exception ex)
            {
                ConfigManager.Logger.LogError(ex);
            }
        });
    }

    public static void TeleportToCoords(float x, float y, float z)
    {
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            try
            {
                Character localCharacter = Character.localCharacter;
                if (localCharacter == null || localCharacter.data.dead)
                {
                    Logger.LogWarning("[Teleport] Local character is null or dead. Aborting teleport.");
                    return;
                }

                PhotonView photonView = localCharacter.photonView;
                if (photonView == null)
                    return;

                Vector3 target = new Vector3(x, y, z);
                photonView.RPC("WarpPlayerRPC", RpcTarget.All, new object[]
                {
                target, true
                });

                ConfigManager.Logger.LogInfo($"[Teleport] Teleported to {target}");
            }
            catch (Exception ex)
            {
                ConfigManager.Logger.LogError("[Teleport] Exception: " + ex);
            }
        });
    }

    public static bool hasInitializedLuggageList = false;

    public static void EnsureLuggageListInitialized()
    {
        if (!hasInitializedLuggageList && Character.localCharacter != null)
        {
            hasInitializedLuggageList = true;
            RefreshLuggageList();
        }
    }


    public static void RefreshLuggageList()
    {
        try
        {
            Globals.luggageLabels.Clear();
            Globals.luggageObject.Clear();
            Globals.selectedLuggageIndex = -1;

            var localChar = Character.localCharacter;
            if (localChar == null)
                return;

            var luggageList = Luggage.ALL_LUGGAGE;
            if (luggageList == null || luggageList.Count == 0)
                return;

            var allLuggage = new List<(Luggage lug, float distance)>();
            Vector3 headPos = localChar.Head;

            for (int i = 0; i < luggageList.Count; i++)
            {
                try
                {
                    var lug = luggageList[i];
                    if (lug == null) continue;

                    float distance = Vector3.Distance(headPos, lug.Center());
                    if (distance <= 300)
                    {
                        allLuggage.Add((lug, distance));
                    }
                }
                catch
                {
                    continue;
                }
            }

            allLuggage.Sort((a, b) => a.distance.CompareTo(b.distance));

            foreach (var (lug, distance) in allLuggage)
            {
                try
                {
                    string name = lug.displayName ?? "Unnamed";
                    Globals.luggageLabels.Add($"{name} [{distance:F1}m]");
                    Globals.luggageObject.Add(lug);
                }
                catch
                {
                    continue;
                }
            }

            Logger.LogInfo($"[Luggage] Refreshed. Found {Globals.luggageLabels.Count} nearby.");
        }
        catch (Exception ex)
        {
            ConfigManager.Logger.LogError($"[Luggage] RefreshLuggageList error: {ex.Message}");
        }
    }

    public static void OpenAllNearbyLuggage()
    {
        UnityMainThreadDispatcher.Enqueue(() =>
        {
            int opened = 0;

            for (int i = 0; i < Globals.luggageObject.Count; i++)
            {
                var luggage = Globals.luggageObject[i];
                if (luggage == null) continue;

                var view = luggage.GetComponent<PhotonView>();
                if (view != null)
                {
                    view.RPC("OpenLuggageRPC", RpcTarget.All, new object[] { true });
                    opened++;
                }
            }

            Logger.LogInfo($"[Luggage] Requested open for {opened} nearby containers.");
        });
    }

    public static void OpenLuggage(int index)
    {
        if (index < 0 || index >= Globals.luggageObject.Count)
            return;

        var luggage = Globals.luggageObject[index];
        if (luggage == null)
            return;

        UnityMainThreadDispatcher.Enqueue(() =>
        {
            try
            {
                PhotonView view = luggage.GetComponent<PhotonView>();
                if (view != null)
                {
                    view.RPC("OpenLuggageRPC", RpcTarget.All, new object[] { true });
                    Logger.LogInfo($"[Luggage] Sent OpenLuggageRPC for: {luggage.displayName}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Luggage] Open failed: {ex}");
            }
        });
    }

    public static void SpawnScoutmasterForPlayer(int playerIndex)
    {
        UnityMainThreadDispatcher.Enqueue(async () =>
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Logger.LogWarning("[Scoutmaster] Only the MasterClient can spawn the Scoutmaster.");
                return;
            }

            if (playerIndex < 0 || playerIndex >= Character.AllCharacters.Count)
            {
                Logger.LogWarning("[Scoutmaster] Invalid player index.");
                return;
            }

            Character targetCharacter = Character.AllCharacters[playerIndex];
            Vector3 targetPos = targetCharacter.transform.position;
            Vector3 spawnOrigin = targetPos + new Vector3(UnityEngine.Random.Range(-10f, 10f), 25f, UnityEngine.Random.Range(-10f, 10f));
            Vector3 down = Vector3.down;

            if (Physics.Raycast(spawnOrigin, down, out RaycastHit hit, 100f, ~0))
            {
                Vector3 spawnPoint = hit.point + Vector3.up * 1f;
                Quaternion rotation = Quaternion.identity;

                GameObject scoutObj = PhotonNetwork.InstantiateRoomObject("Character_Scoutmaster", spawnPoint, rotation, 0, null);
                var character = scoutObj.GetComponent<Character>();
                if (character != null)
                    character.data.spawnPoint = character.transform;

                await Task.Delay(100);

                var scoutmaster = scoutObj.GetComponent<Scoutmaster>();
                if (scoutmaster != null)
                {
                    try
                    {
                        var method = typeof(Scoutmaster).GetMethod("SetCurrentTarget", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (method != null)
                        {
                            method.Invoke(scoutmaster, new object[] { targetCharacter, 15f });
                            Logger.LogInfo($"[Scoutmaster] Target set to {targetCharacter.characterName}");
                        }
                        else
                        {
                            Logger.LogWarning("[Scoutmaster] Reflection failed — method not found.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("[Scoutmaster] Reflection error: " + ex);
                    }
                }
            }
            else
            {
                Logger.LogWarning("[Scoutmaster] No valid ground to spawn.");
            }
        });
    }
}
