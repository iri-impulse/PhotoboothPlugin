using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using Photobooth.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Photobooth.Controls
{
    internal class SnapshotDataController
    {
        private static readonly JsonSerializerOptions _SerializerOptions = new JsonSerializerOptions
        {
            IncludeFields = true
        };

        private const string FileName = "Snapshots.json";
        private string _filePath = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, FileName);
        private Dictionary<uint, List<PortraitSnapshot>> _snapshotsInternal = new Dictionary<uint, List<PortraitSnapshot>>();
        private bool _snapshotsAreLoaded = false;
        public Dictionary<uint, List<PortraitSnapshot>> Snapshots { get
            {
                if (!_snapshotsAreLoaded)
                {
                    LoadSnapshots();
                }

                return _snapshotsInternal;
            } }



        public unsafe void StoreCurrentSnapshot(ExportedPortraitData data, uint classJobId, Guid id)
        {
            var classJobName = Plugin.DataManager.GetExcelSheet<ClassJob>().GetRow(classJobId).NameEnglish.ToString();
            var serializedSnapshotData = System.Text.Json.JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                IncludeFields = true
            });

            var snapshots = Snapshots;
            if (!snapshots.ContainsKey(classJobId))
            {
                snapshots[classJobId] = new List<PortraitSnapshot>();
            }

            var existing = snapshots[classJobId].FirstOrDefault(snapshot => snapshot.Id == id);
            if (existing == null)
            {
                var newSnapshot = new PortraitSnapshot(classJobId, classJobName, serializedSnapshotData);
                snapshots[classJobId].Add(newSnapshot);
                Plugin.Log.Info($"Portrait for job {classJobName} created: {serializedSnapshotData}");

            }
            else
            {                
                existing.TakenAt = DateTime.Now;
                existing.SerializedSnapshot = serializedSnapshotData;
                Plugin.Log.Info($"Portrait for job {classJobName} updated: {serializedSnapshotData}");
            }
        }

        private Dictionary<uint, List<PortraitSnapshot>> LoadSnapshots()
        {
            if (!File.Exists(_filePath))
            {
                Plugin.Log.Info("Snapshot file created");
                File.WriteAllText(_filePath, JsonSerializer.Serialize(_snapshotsInternal, _SerializerOptions));
            }
            else
            {
                var serialized = File.ReadAllText(_filePath);
                _snapshotsInternal = JsonSerializer.Deserialize<Dictionary<uint, List<PortraitSnapshot>>>(serialized) ?? new();
                Plugin.Log.Info($"Snapshots file loaded");
            }
            
            _snapshotsAreLoaded = true;

            return _snapshotsInternal;
        }
    }
}
