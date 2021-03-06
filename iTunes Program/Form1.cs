﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Collections;
using iTunesLib;
using System.IO;

namespace iTunes_Program
{
    public partial class Form1 : Form
    {
        private volatile bool _shouldStop;
        private Thread worker;

        public Form1()
        {
            InitializeComponent();
        }

        private void RemoveOnestar()
        {
            //create a reference to iTunes
            iTunesApp iTunes = new iTunesApp();

            //get a reference to the collection of all tracks
            IITTrackCollection tracks = iTunes.LibraryPlaylist.Tracks;

            int trackCount = tracks.Count;
            int numberChecked = 0;
            int numberOnestar = 0;
            ArrayList tracksToRemove = new ArrayList();

            //setup the progress control
            this.SetupProgress(trackCount);

            for (int i = trackCount; i > 0; i--)
            {
                if (tracks[i].Kind == ITTrackKind.ITTrackKindFile)
                {
                    if (!this._shouldStop)
                    {
                        numberChecked++;
                        this.IncrementProgress();
                       
                        if (tracks[i].Rating == 20)
                        {
                                IITFileOrCDTrack fileTrack = (IITFileOrCDTrack)tracks[i];
                                numberOnestar++;
                                tracksToRemove.Add(tracks[i]);
                        }
                        
                    }
                }
            }

            this.SetupProgress(tracksToRemove.Count);

            for (int i = 0; i < tracksToRemove.Count; i++)
            {
                IITFileOrCDTrack track = (IITFileOrCDTrack)tracksToRemove[i];
                this.IncrementProgress();
                this.AddTrackToList((IITFileOrCDTrack)tracksToRemove[i]);
                if (this.checkBoxRemove.Checked)
                {
                    //oldloc = PATH:\TO\FILE\Artist - Song.ext
                    //newloc = Artist - Song.ext
                    //newloc = M:\Other Music\TrashedMusic\Artist - Song.ext
                    string oldloc = track.Location;
                    string newloc = track.Location.Split('\\')[track.Location.Split('\\').Length - 1];
                    newloc = @"M:\Other Music\TrashedMusic\" + newloc;
                    
                    //TrashTrack moves file @ oldloc to file @ newloc - returns 'true' on success
                    if (!TrashTrack(oldloc,newloc))
                    {
                        this._shouldStop = true;
                        break;
                    }
                    else
                    {
                        track.Delete();
                    }
                }
            }

            this.UpdateLabel("Checked " + numberChecked.ToString() + " tracks and " + numberOnestar.ToString() + " Onestar tracks found.");
            this.SetupProgress(1);
        }

        private void RemoveDuplicates()
        {
            //create a reference to iTunes
            iTunesApp iTunes = new iTunesApp();

            //get a reference to the collection of all tracks
            IITTrackCollection tracks = iTunes.LibraryPlaylist.Tracks;

            int trackCount = tracks.Count;
            int numberChecked = 0;
            int numberDuplicateFound = 0;
            Dictionary<string, IITTrack> trackCollection = new Dictionary<string, IITTrack>();
            ArrayList tracksToRemove = new ArrayList();

            //setup the progress control
            this.SetupProgress(trackCount);

            for (int i = trackCount; i > 0; i--)
            {
                if (tracks[i].Kind == ITTrackKind.ITTrackKindFile)
                {
                    if (!this._shouldStop)
                    {
                        numberChecked++;
                        this.IncrementProgress();
                        this.UpdateLabel("Checking track # " + numberChecked.ToString() + " - " + tracks[i].Name);
                        string trackKey = tracks[i].Name + tracks[i].Artist + tracks[i].Album;

                        if (!trackCollection.ContainsKey(trackKey))
                        {
                            trackCollection.Add(trackKey, tracks[i]);
                        }
                        else
                        {
                            if (trackCollection[trackKey].Album != tracks[i].Album || trackCollection[trackKey].Artist != tracks[i].Artist)
                            {
                                trackCollection.Add(trackKey, tracks[i]);
                            }
                            else if (trackCollection[trackKey].BitRate > tracks[i].BitRate)
                            {
                                IITFileOrCDTrack fileTrack = (IITFileOrCDTrack)tracks[i];
                                numberDuplicateFound++;
                                tracksToRemove.Add(tracks[i]);
                            }
                            else
                            {
                                IITFileOrCDTrack fileTrack = (IITFileOrCDTrack)tracks[i];
                                trackCollection[trackKey] = fileTrack;
                                numberDuplicateFound++;
                                tracksToRemove.Add(tracks[i]);
                            }
                        }
                    }
                }
            }

            this.SetupProgress(tracksToRemove.Count);

            for (int i = 0; i < tracksToRemove.Count; i++)
            {
                IITFileOrCDTrack track = (IITFileOrCDTrack)tracksToRemove[i];
                this.UpdateLabel("Removing " + track.Name);
                this.IncrementProgress();
                this.AddTrackToList((IITFileOrCDTrack)tracksToRemove[i]);

                if (this.checkBoxRemove.Checked)
                {
                    track.Delete();
                }
            }

            this.UpdateLabel("Checked " + numberChecked.ToString() + " tracks and " + numberDuplicateFound.ToString() + " duplicate tracks found.");
            this.SetupProgress(1);
        }

        private void FindDeadTracks()
        {
            //create a reference to iTunes
            iTunesApp iTunes = new iTunesApp();

            //get a reference to the collection of all tracks
            IITTrackCollection tracks = iTunes.LibraryPlaylist.Tracks;

            int trackCount = tracks.Count;
            int numberChecked = 0;
            int numberDeadFound = 0;

            //setup the progress control
            this.SetupProgress(trackCount);

            for (int i = trackCount; i > 0; i--)
            {
                if (!this._shouldStop)
                {
                    IITTrack track = tracks[i];
                    numberChecked++;
                    this.IncrementProgress();
                    this.UpdateLabel("Checking track # " + numberChecked.ToString() + " - " + track.Name);

                    if (track.Kind == ITTrackKind.ITTrackKindFile)
                    {
                        IITFileOrCDTrack fileTrack = (IITFileOrCDTrack)track;

                        //if the file doesn't exist, we'll delete it from iTunes
                        if (fileTrack.Location == String.Empty)
                        {
                            numberDeadFound++;
                            this.AddTrackToList(fileTrack);

                            if (this.checkBoxRemove.Checked)
                            {
                                fileTrack.Delete();
                            }
                        }
                        else if (!System.IO.File.Exists(fileTrack.Location))
                        {
                            numberDeadFound++;
                            this.AddTrackToList(fileTrack);

                            if (this.checkBoxRemove.Checked)
                            {
                                fileTrack.Delete();
                            }
                        }
                    }
                }
            }

            this.UpdateLabel("Checked " + numberChecked.ToString() + " tracks and " + numberDeadFound.ToString() + " dead tracks found.");
            this.SetupProgress(1);
        }

        private void CompareMusicDir()
        {
            //create a reference to iTunes
            iTunesApp iTunes = new iTunesApp();

            //get a reference to the collection of all tracks
            IITTrackCollection tracks = iTunes.LibraryPlaylist.Tracks;

            int trackCount = tracks.Count;
            int numberChecked = 0;
            List<string> filesInLib = new List<string>();

            //setup the progress control
            this.SetupProgress(trackCount);

            for (int i = trackCount; i > 0; i--)
            {
                if (tracks[i].Kind == ITTrackKind.ITTrackKindFile)
                {
                    if (!this._shouldStop)
                    {
                        numberChecked++;
                        IITFileOrCDTrack fileTrack = (IITFileOrCDTrack)tracks[i];
                        filesInLib.Add(fileTrack.Location.ToLower());

                    }
                }
            } //end for

            string dirPath = @"D:\Music";

            List<string> dirs = new List<string>(Directory.EnumerateDirectories(dirPath));
            List<string> files = new List<string>();

            foreach (var dir in dirs)
            {
                foreach (var fileindir in Directory.EnumerateFiles(dir))
                {
                        files.Add(fileindir.ToLower());
                }
            }

            filesInLib.Sort();
            files.Sort();

            IEnumerable<string> diff = files.Except(filesInLib);

            foreach(string s in diff)
            {
                if(!s.EndsWith(".db")) MessageBox.Show(s);
            }


        } 
        //end CompareMusicDir

        #region Message Handlers
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this._shouldStop = true;
            this.buttonCancel.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.label1.Text = "";
            this.buttonCancel.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this._shouldStop = false;
            this.buttonCancel.Enabled = true;
            this.listView1.Items.Clear();

            this.worker = new Thread(this.FindDeadTracks);
            this.worker.Start();
        }
        #endregion

        #region Delegate Callbacks
        //delagates for thread-safe access to UI components
        delegate void SetupProgressCallback(int max);
        delegate void IncrementProgressCallback();
        delegate void UpdateLabelCallback(string text);
        delegate void CompleteOperationCallback(string message);
        delegate void AddTrackToListCallback(IITFileOrCDTrack fileTrack);

        private void IncrementProgress()
        {
            if (this.progressBar1.InvokeRequired)
            {
                IncrementProgressCallback cb = new IncrementProgressCallback(IncrementProgress);
                this.Invoke(cb, new object[] { });
            }
            else
            {
                this.progressBar1.PerformStep();
            }
        }

        private void UpdateLabel(string text)
        {
            if (this.label1.InvokeRequired)
            {
                UpdateLabelCallback cb = new UpdateLabelCallback(UpdateLabel);
                this.Invoke(cb, new object[] { text });
            }
            else
            {
                this.label1.Text = text;
            }
        }

        private bool TrashTrack(string path1, string path2)
        {
            bool retVal = true;

            try
            {
                // Ensure that the target does not exist. 
                if (File.Exists(path2)) File.Delete(path2);
                // Move the file.
                File.Move(path1, path2);
            }
            catch (Exception e)
            {
                string message = e.ToString();
                string caption = "Error Detected, continue?";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result;
                result = MessageBox.Show(message, caption, buttons);

                if (result == System.Windows.Forms.DialogResult.No)
                {
                    retVal = false;
                }
            }
            return retVal;
        }   

        private void CompleteOperation(string message)
        {
            if (this.label1.InvokeRequired)
            {
                CompleteOperationCallback cb = new CompleteOperationCallback(CompleteOperation);
                this.Invoke(cb, new object[] { message });
            }
            else
            {
                this.label1.Text = message;
            }
        }

        private void AddTrackToList(IITFileOrCDTrack fileTrack)
        {
            if (this.listView1.InvokeRequired)
            {
                AddTrackToListCallback cb = new AddTrackToListCallback(AddTrackToList);
                this.Invoke(cb, new object[] { fileTrack });
            }
            else
            {
                this.listView1.Items.Add(new ListViewItem(new string[] { fileTrack.Name, fileTrack.Artist, fileTrack.Location, fileTrack.BitRate.ToString() }));
            }
        }

        private void SetupProgress(int max)
        {
            if (this.progressBar1.InvokeRequired)
            {
                SetupProgressCallback cb = new SetupProgressCallback(SetupProgress);
                this.Invoke(cb, new object[] { max });
            }
            else
            {
                this.progressBar1.Maximum = max;
                this.progressBar1.Minimum = 1;
                this.progressBar1.Step = 1;
                this.progressBar1.Value = 1;
            }
        }
        #endregion

        private void button2_Click(object sender, EventArgs e)
        {
            this._shouldStop = false;
            this.buttonCancel.Enabled = true;
            this.listView1.Items.Clear();

            this.worker = new Thread(this.RemoveDuplicates);
            this.worker.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this._shouldStop = false;
            this.buttonCancel.Enabled = true;
            this.listView1.Items.Clear();

            this.worker = new Thread(this.RemoveOnestar);
            this.worker.Start();
        }

        private void compareButton_Click(object sender, EventArgs e)
        {
            CompareMusicDir();
        }


    }
}
