//============================================================================
// BDInfo - Blu-ray Video and Audio Analysis Tool
// Copyright © 2010 Cinema Squid
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//=============================================================================

namespace BDInfo
{
    /// <summary>
    /// Static settings class for BDInfo CLI.
    /// Settings use sensible defaults for command-line operation.
    /// </summary>
    public static class BDInfoSettings
    {
        // Report generation settings
        public static bool GenerateStreamDiagnostics { get; set; } = true;
        public static bool ExtendedStreamDiagnostics { get; set; } = true;
        public static bool GenerateTextSummary { get; set; } = true;
        public static bool AutosaveReport { get; set; } = true;
        public static bool GenerateFrameDataFile { get; set; } = false;

        // Playlist filtering settings
        public static bool FilterLoopingPlaylists { get; set; } = false;
        public static bool FilterShortPlaylists { get; set; } = false;
        public static int FilterShortPlaylistsValue { get; set; } = 0;

        // Display settings
        public static bool KeepStreamOrder { get; set; } = false;
        public static bool EnableSSIF { get; set; } = true;
        public static bool DisplayChapterCount { get; set; } = false;

        // Image prefix settings
        public static bool UseImagePrefix { get; set; } = false;
        public static string UseImagePrefixValue { get; set; } = "";

        // State (not persisted)
        public static string LastPath { get; set; } = "";

        /// <summary>
        /// No-op for CLI version - settings are not persisted.
        /// </summary>
        public static void SaveSettings()
        {
            // Settings are not persisted in CLI version
        }
    }
}
