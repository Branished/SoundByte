﻿/* |----------------------------------------------------------------|
 * | Copyright (c) 2017, Grid Entertainment                         |
 * | All Rights Reserved                                            |
 * |                                                                |
 * | This source code is to only be used for educational            |
 * | purposes. Distribution of SoundByte source code in             |
 * | any form outside this repository is forbidden. If you          |
 * | would like to contribute to the SoundByte source code, you     |
 * | are welcome.                                                   |
 * |----------------------------------------------------------------|
 */

using Newtonsoft.Json;

namespace SoundByte.API.Endpoints
{
    /// <summary>
    ///     A stream collection containing all items that may be on the users stream
    /// </summary>
    [JsonObject]
    public class StreamItem
    {
        /// <summary>
        ///     Track detail
        /// </summary>
        [JsonProperty("track")]
        public Track Track { get; set; }

        /// <summary>
        ///     User detail
        /// </summary>
        [JsonProperty("user")]
        public User User { get; set; }

        /// <summary>
        ///     Playlist detail
        /// </summary>
        [JsonProperty("playlist")]
        public Playlist Playlist { get; set; }

        /// <summary>
        ///     When this object was created
        /// </summary>
        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        /// <summary>
        ///     What type of object this is
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}