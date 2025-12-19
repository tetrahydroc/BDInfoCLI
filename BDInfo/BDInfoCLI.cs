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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace BDInfo
{
    /// <summary>
    /// Command-line interface for BDInfo disc analysis.
    /// </summary>
    public class BDInfoCLI
    {
        private BDROM BDROM = null;
        private List<TSPlaylistFile> selectedPlaylists = new List<TSPlaylistFile>();
        private ScanBDROMResult ScanResult = new ScanBDROMResult();

        /// <summary>
        /// Initialize and scan a BD-ROM path (directory or ISO file).
        /// </summary>
        public void InitBDROM(string path)
        {
            try
            {
                BDROM = new BDROM(path);
                BDROM.StreamClipFileScanError += BDROM_StreamClipFileScanError;
                BDROM.StreamFileScanError += BDROM_StreamFileScanError;
                BDROM.PlaylistFileScanError += BDROM_PlaylistFileScanError;
                BDROM.Scan();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error scanning disc: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get the BDROM object for direct access.
        /// </summary>
        public BDROM GetBDROM()
        {
            return BDROM;
        }

        /// <summary>
        /// Get the selected playlists.
        /// </summary>
        public List<TSPlaylistFile> GetSelectedPlaylists()
        {
            return selectedPlaylists;
        }

        /// <summary>
        /// Get the scan result.
        /// </summary>
        public ScanBDROMResult GetScanResult()
        {
            return ScanResult;
        }

        protected bool BDROM_PlaylistFileScanError(TSPlaylistFile playlistFile, Exception ex)
        {
            Console.Error.WriteLine($"Error scanning playlist {playlistFile.Name}: {ex.Message}");
            Console.Write("Continue scanning? (y/n): ");
            string response = Console.ReadLine();
            return response?.ToLower() == "y";
        }

        protected bool BDROM_StreamFileScanError(TSStreamFile streamFile, Exception ex)
        {
            Console.Error.WriteLine($"Error scanning stream file {streamFile.Name}: {ex.Message}");
            Console.Write("Continue scanning? (y/n): ");
            string response = Console.ReadLine();
            return response?.ToLower() == "y";
        }

        protected bool BDROM_StreamClipFileScanError(TSStreamClipFile streamClipFile, Exception ex)
        {
            Console.Error.WriteLine($"Error scanning stream clip file {streamClipFile.Name}: {ex.Message}");
            Console.Write("Continue scanning? (y/n): ");
            string response = Console.ReadLine();
            return response?.ToLower() == "y";
        }

        /// <summary>
        /// Load specific playlists by name.
        /// </summary>
        public void LoadPlaylists(List<string> inputPlaylists)
        {
            selectedPlaylists = new List<TSPlaylistFile>();
            foreach (string playlistName in inputPlaylists)
            {
                string name = playlistName.ToUpper();
                if (BDROM.PlaylistFiles.ContainsKey(name))
                {
                    if (!selectedPlaylists.Contains(BDROM.PlaylistFiles[name]))
                    {
                        selectedPlaylists.Add(BDROM.PlaylistFiles[name]);
                    }
                }
            }

            if (selectedPlaylists.Count == 0)
            {
                throw new Exception("No matching playlists found on BD");
            }
        }

        /// <summary>
        /// Load playlists - either whole disc or interactive selection.
        /// </summary>
        public void LoadPlaylists(bool wholeDisc = false)
        {
            selectedPlaylists = new List<TSPlaylistFile>();

            if (BDROM == null) return;

            bool hasHiddenTracks = false;
            List<List<TSPlaylistFile>> groups = new List<List<TSPlaylistFile>>();

            TSPlaylistFile[] sortedPlaylistFiles = new TSPlaylistFile[BDROM.PlaylistFiles.Count];
            BDROM.PlaylistFiles.Values.CopyTo(sortedPlaylistFiles, 0);
            Array.Sort(sortedPlaylistFiles, ComparePlaylistFiles);

            foreach (TSPlaylistFile playlist1 in sortedPlaylistFiles)
            {
                if (!playlist1.IsValid) continue;

                int matchingGroupIndex = 0;
                for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
                {
                    List<TSPlaylistFile> group = groups[groupIndex];
                    foreach (TSPlaylistFile playlist2 in group)
                    {
                        if (!playlist2.IsValid) continue;

                        foreach (TSStreamClip clip1 in playlist1.StreamClips)
                        {
                            foreach (TSStreamClip clip2 in playlist2.StreamClips)
                            {
                                if (clip1.Name == clip2.Name)
                                {
                                    matchingGroupIndex = groupIndex + 1;
                                    break;
                                }
                            }
                            if (matchingGroupIndex > 0) break;
                        }
                        if (matchingGroupIndex > 0) break;
                    }
                    if (matchingGroupIndex > 0) break;
                }
                if (matchingGroupIndex > 0)
                {
                    groups[matchingGroupIndex - 1].Add(playlist1);
                }
                else
                {
                    groups.Add(new List<TSPlaylistFile> { playlist1 });
                }
            }

            Console.WriteLine(String.Format("{0,-4}{1,-7}{2,-15}{3,-10}{4,-16}{5,-16}\n",
                "#", "Group", "Playlist File", "Length", "Estimated Bytes", "Measured Bytes"));

            int playlistIdx = 1;
            Dictionary<int, TSPlaylistFile> playlistDict = new Dictionary<int, TSPlaylistFile>();

            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                List<TSPlaylistFile> group = groups[groupIndex];
                group.Sort(ComparePlaylistFiles);

                foreach (TSPlaylistFile playlist in group)
                {
                    if (!playlist.IsValid) continue;

                    playlistDict[playlistIdx] = playlist;
                    if (wholeDisc)
                        selectedPlaylists.Add(playlist);

                    if (playlist.HasHiddenTracks)
                    {
                        hasHiddenTracks = true;
                    }

                    string groupString = (groupIndex + 1).ToString();

                    TimeSpan playlistLengthSpan = new TimeSpan((long)(playlist.TotalLength * 10000000));
                    string length = string.Format(
                        "{0:D2}:{1:D2}:{2:D2}",
                        playlistLengthSpan.Hours,
                        playlistLengthSpan.Minutes,
                        playlistLengthSpan.Seconds);

                    string fileSize;
                    if (BDInfoSettings.EnableSSIF && playlist.InterleavedFileSize > 0)
                    {
                        fileSize = playlist.InterleavedFileSize.ToString("N0");
                    }
                    else if (playlist.FileSize > 0)
                    {
                        fileSize = playlist.FileSize.ToString("N0");
                    }
                    else
                    {
                        fileSize = "-";
                    }

                    string fileSize2;
                    if (playlist.TotalAngleSize > 0)
                    {
                        fileSize2 = playlist.TotalAngleSize.ToString("N0");
                    }
                    else
                    {
                        fileSize2 = "-";
                    }

                    Console.WriteLine(String.Format("{0,-4:G}{1,-7}{2,-15}{3,-10}{4,-16}{5,-16}",
                        playlistIdx.ToString(), groupString, playlist.Name, length, fileSize, fileSize2));
                    playlistIdx++;
                }
            }

            if (hasHiddenTracks)
            {
                Console.WriteLine("(*) Some playlists on this disc have hidden tracks. These tracks are marked with an asterisk.");
            }
            if (wholeDisc)
                return;

            for (int selectedIdx; (selectedIdx = GetIntIndex(1, playlistIdx - 1)) > 0;)
            {
                selectedPlaylists.Add(playlistDict[selectedIdx]);
                Console.WriteLine($"Added {selectedIdx}");
            }
            if (selectedPlaylists.Count == 0)
            {
                Console.WriteLine("No playlists selected. Exiting.");
                Environment.Exit(0);
            }
        }

        private static int GetIntIndex(int min, int max)
        {
            string response;
            int resp = -1;
            do
            {
                while (Console.KeyAvailable)
                    Console.ReadKey();

                Console.Write("Select (q when finished): ");
                response = Console.ReadLine();
                if (response == "q")
                    return -1;

                try
                {
                    resp = int.Parse(response);
                }
                catch (Exception)
                {
                    Console.WriteLine("Invalid Input!");
                    continue;
                }

                if (resp > max || resp < min)
                {
                    Console.WriteLine("Invalid Selection!");
                }
            } while (resp > max || resp < min);

            Console.WriteLine();
            return resp;
        }

        /// <summary>
        /// Scan the BD-ROM streams for bitrate analysis.
        /// </summary>
        public void ScanBDROM()
        {
            ScanResult = new ScanBDROMResult { ScanException = new Exception("Scan is still running.") };

            List<TSStreamFile> streamFiles = new List<TSStreamFile>();
            List<string> streamNames;
            Console.WriteLine("Preparing to analyze the following:");

            foreach (TSPlaylistFile playlist in selectedPlaylists)
            {
                Console.Write($"{playlist.Name} --> ");
                streamNames = new List<string>();
                foreach (TSStreamClip clip in playlist.StreamClips)
                {
                    if (!streamFiles.Contains(clip.StreamFile))
                    {
                        streamNames.Add(clip.StreamFile.Name);
                        streamFiles.Add(clip.StreamFile);
                    }
                }
                Console.WriteLine(String.Join(" + ", streamNames));
            }

            Timer timer = null;
            try
            {
                ScanBDROMState scanState = new ScanBDROMState();
                foreach (TSStreamFile streamFile in streamFiles)
                {
                    if (BDInfoSettings.EnableSSIF && streamFile.InterleavedFile != null)
                    {
                        if (streamFile.InterleavedFile.FileInfo != null)
                            scanState.TotalBytes += streamFile.InterleavedFile.FileInfo.Length;
                        else
                            scanState.TotalBytes += streamFile.InterleavedFile.DFileInfo.Length;
                    }
                    else
                    {
                        if (streamFile.FileInfo != null)
                            scanState.TotalBytes += streamFile.FileInfo.Length;
                        else
                            scanState.TotalBytes += streamFile.DFileInfo.Length;
                    }

                    if (!scanState.PlaylistMap.ContainsKey(streamFile.Name))
                    {
                        scanState.PlaylistMap[streamFile.Name] = new List<TSPlaylistFile>();
                    }

                    foreach (TSPlaylistFile playlist in BDROM.PlaylistFiles.Values)
                    {
                        playlist.ClearBitrates();

                        foreach (TSStreamClip clip in playlist.StreamClips)
                        {
                            if (clip.Name == streamFile.Name)
                            {
                                if (!scanState.PlaylistMap[streamFile.Name].Contains(playlist))
                                {
                                    scanState.PlaylistMap[streamFile.Name].Add(playlist);
                                }
                            }
                        }
                    }
                }

                timer = new Timer(ScanBDROMProgress, scanState, 1000, 1000);
                Console.WriteLine("\n{0,16}{1,-15}{2,-13}{3}", "", "File", "Elapsed", "Remaining");

                foreach (TSStreamFile streamFile in streamFiles)
                {
                    scanState.StreamFile = streamFile;

                    Thread thread = new Thread(ScanBDROMThread);
                    thread.Start(scanState);
                    while (thread.IsAlive)
                    {
                        Thread.Sleep(250);
                    }
                    if (streamFile.FileInfo != null)
                        scanState.FinishedBytes += streamFile.FileInfo.Length;
                    else
                        scanState.FinishedBytes += streamFile.DFileInfo.Length;
                    if (scanState.Exception != null)
                    {
                        ScanResult.FileExceptions[streamFile.Name] = scanState.Exception;
                    }
                }
                ScanResult.ScanException = null;
            }
            catch (Exception ex)
            {
                ScanResult.ScanException = ex;
            }
            finally
            {
                Console.WriteLine();
                timer?.Dispose();
            }
        }

        private void ScanBDROMThread(object parameter)
        {
            ScanBDROMState scanState = (ScanBDROMState)parameter;
            try
            {
                TSStreamFile streamFile = scanState.StreamFile;
                List<TSPlaylistFile> playlists = scanState.PlaylistMap[streamFile.Name];
                streamFile.Scan(playlists, true);
            }
            catch (Exception ex)
            {
                scanState.Exception = ex;
            }
        }

        private void ScanBDROMProgress(object state)
        {
            ScanBDROMState scanState = (ScanBDROMState)state;

            try
            {
                long finishedBytes = scanState.FinishedBytes;
                if (scanState.StreamFile != null)
                {
                    finishedBytes += scanState.StreamFile.Size;
                }

                double progress = ((double)finishedBytes / scanState.TotalBytes);
                int progressValue = (int)Math.Round(progress * 100);
                if (progressValue < 0) progressValue = 0;
                if (progressValue > 100) progressValue = 100;

                TimeSpan elapsedTime = DateTime.Now.Subtract(scanState.TimeStarted);
                TimeSpan remainingTime;
                if (progress > 0 && progress < 1)
                {
                    remainingTime = new TimeSpan(
                        (long)((double)elapsedTime.Ticks / progress) - elapsedTime.Ticks);
                }
                else
                {
                    remainingTime = new TimeSpan(0);
                }

                string elapsedTimeString = string.Format(CultureInfo.InvariantCulture,
                    "{0:D2}:{1:D2}:{2:D2}",
                    elapsedTime.Hours,
                    elapsedTime.Minutes,
                    elapsedTime.Seconds);

                string remainingTimeString = string.Format(CultureInfo.InvariantCulture,
                    "{0:D2}:{1:D2}:{2:D2}",
                    remainingTime.Hours,
                    remainingTime.Minutes,
                    remainingTime.Seconds);

                if (scanState.StreamFile != null)
                {
                    Console.Write("Scanning {0,3:d}% - {1,10} {2,12}  |  {3}\r",
                        progressValue, scanState.StreamFile.DisplayName, elapsedTimeString, remainingTimeString);
                }
                else
                {
                    Console.Write("Scanning {0,3}% - \t{1,10}  |  {2}...\r",
                        progressValue, elapsedTimeString, remainingTimeString);
                }
            }
            catch { }
        }

        public static int ComparePlaylistFiles(TSPlaylistFile x, TSPlaylistFile y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null && y != null)
            {
                return 1;
            }
            else if (x != null && y == null)
            {
                return -1;
            }
            else
            {
                if (x.TotalLength > y.TotalLength)
                {
                    return -1;
                }
                else if (y.TotalLength > x.TotalLength)
                {
                    return 1;
                }
                else
                {
                    return x.Name.CompareTo(y.Name);
                }
            }
        }
    }

    /// <summary>
    /// State object for BD-ROM scanning progress tracking.
    /// </summary>
    public class ScanBDROMState
    {
        public long TotalBytes = 0;
        public long FinishedBytes = 0;
        public DateTime TimeStarted = DateTime.Now;
        public TSStreamFile StreamFile = null;
        public Dictionary<string, List<TSPlaylistFile>> PlaylistMap = new Dictionary<string, List<TSPlaylistFile>>();
        public Exception Exception = null;
    }

    /// <summary>
    /// Result object containing scan results and any exceptions.
    /// </summary>
    public class ScanBDROMResult
    {
        public Exception ScanException = new Exception("Scan has not been run.");
        public Dictionary<string, Exception> FileExceptions = new Dictionary<string, Exception>();
    }
}
