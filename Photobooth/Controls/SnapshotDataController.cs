using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Photobooth.Controls
{
    internal static class SnapshotDataController
    {
        private const string FileName = "Snapshots.json";
        public static unsafe void StoreCurrentSnapshot(ExportedPortraitData data, uint classJobId)
        {
            var classJobName = Plugin.DataManager.GetExcelSheet<ClassJob>().GetRow(classJobId).NameEnglish.ToString();
            var dateTime = DateTime.Now;
            var filePath = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, FileName);
            Plugin.Log.Warning(filePath);

        }
    }
}
