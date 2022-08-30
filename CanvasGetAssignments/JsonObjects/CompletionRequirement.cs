﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CanvasGetAssignments.JsonObjects
{
    internal class CompletionRequirement
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("completed")]
        public bool IsCompleted { get; set; }
    }
}