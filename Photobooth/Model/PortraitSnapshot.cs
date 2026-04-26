using System;
using System.Collections.Generic;
using System.Text;

namespace Photobooth.Model
{
    internal class PortraitSnapshot
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime TakenAt { get; set; }
        public uint ClassJobId { get; set; }

        public string ClassJobName { get; set; }

        public string SerializedSnapshot { get; set;  }

        public string Race { get; set; }

        public string TribeName { get; set; }

        public string Gender { get; set; }

        public PortraitSnapshot(uint classJobId, string classJobName, string race, string tribeName, string gender, string serializedSnapshot)
        {
            TakenAt = DateTime.Now;
            ClassJobId = classJobId;
            ClassJobName = classJobName;
            Race = race;
            TribeName = tribeName;
            Gender = gender;
            SerializedSnapshot = serializedSnapshot;
        }
    }
}
