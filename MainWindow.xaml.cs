using Microsoft.Win32;
using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using TagLib.Mpeg;
using TagLib;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace FlashAudio
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WaveOutEvent waveOut;
        AudioFileReader mp3;
        VorbisWaveReader vorbis;

        public ObservableCollection<InfoMusic> InfoMusics { get; set; }

        //WaveFileReader wave;
        private bool IsPlaying = true;
        private bool IsReturned = false;

        public MainWindow()
        {
            InitializeComponent();
            InfoMusics = new ObservableCollection<InfoMusic>();
            ListMusic.ItemsSource = InfoMusics;
            waveOut = new WaveOutEvent();
            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
        }
        public MainWindow(string[] fileName)
        {
            InitializeComponent();
            InfoMusics = new ObservableCollection<InfoMusic>();
            ListMusic.ItemsSource = InfoMusics;
            waveOut = new WaveOutEvent();
            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
            if (!string.IsNullOrWhiteSpace(fileName.FirstOrDefault()) && System.IO.File.Exists(fileName.FirstOrDefault()))
            {
                OpenFileAudio(fileName);
            }
        }

        private void OpenAudio(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog
            {
                Filter = "Audio Files (*.mp3; *.wav; *.m4a; *.ogg; *.flac; *.mp2; *.ac3; *.amr; *.wma; *.aac; *.3gp)|*.mp3;*.wav;*.m4a;*.ogg;*.flac;*.mp2;*.ac3;*.amr;*.wma;*.aac;*.3gp",
                Multiselect = true
            };
            if ((bool)open.ShowDialog())
            {
                OpenFileAudio(open.FileNames);
            }
        }
        private void OpenFileAudio(string[] file)
        {
            int number = 1;
            InfoMusics.Clear();
            foreach (string i in file)
            {
                InfoMusics.Add(new InfoMusic { ID = number, TitleMusic = Path.GetFileNameWithoutExtension(i), LinkFile = i });
                
                number++;
            }
            ListMusic.SelectedIndex = 0;
        }
        private async Task WhilePosition()
        {
            if (vorbis != null)
            {
                await OGGWhilePosition();
                return;
            }
            while (IsPlaying)
            {
                try
                {
                    await Task.Delay(1);
                    slider.Value = mp3.Position;
                    TimeCurrenty.Text = mp3.CurrentTime.ToString(@"hh\:mm\:ss");
                }
                catch {}
            }
        }
        private async Task OGGWhilePosition()
        {
        Schance:
            while (IsPlaying)
            {
                try
                {
                    await Task.Delay(1);
                    slider.Value = vorbis.Position;
                    TimeCurrenty.Text = vorbis.CurrentTime.ToString(@"hh\:mm\:ss");
                }
                catch { goto Schance; }
            }
        }
        private string Tags(AudioFile audio, string file)
        {
            string year = string.Empty, author = string.Empty;
            if (audio.Tag.Year.ToString() != "0")
            {
                year = $"• {audio.Tag.Year.ToString()}";
            }
            try
            {
                author = $"• {audio.Tag.Performers[0]}";
            }
            catch { }
            Picture picture = new Picture(file);
            return $"{Path.GetExtension(file).Replace(".", "")} {year} {author}";
        }

        private void Pause(object sender, RoutedEventArgs e) => waveOut.Pause();
        private void Play(object sender, RoutedEventArgs e)
        {
            if (mp3 != null || vorbis != null)
            {
                if (waveOut.PlaybackState == PlaybackState.Stopped)
                {
                    try
                    {
                        mp3.Position = 0;
                    }
                    catch
                    {
                        try
                        {
                            vorbis.Position = 0;
                        }
                        catch { }
                    }
                }
                waveOut.Play();
            }
        }

        private void Volumes_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mp3 != null || vorbis != null)
            {
                waveOut.Volume = (float)Volumes.Value / 100;
            }
        }

        private async void slider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (mp3 != null)
            {
                mp3.Position = (long)slider.Value < 1000 ? (long)slider.Value : (long)slider.Value - 1000;
            }
            else if (vorbis != null)
            {
                vorbis.Position = (long)slider.Value < 1000 ? (long)slider.Value : (long)slider.Value - 1000;
            }
            IsPlaying = true;
            await WhilePosition();
        }

        private void slider_DragStarted(object sender, DragStartedEventArgs e)
        {
            IsPlaying = false;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            List<string> files = new List<string>((string[])e.Data.GetData(DataFormats.FileDrop));
            string[] file = files.ToArray();

            OpenFileAudio(file);
        }
        private async void NextPlay(string file)
        {
            if (mp3 != null || vorbis != null)
            {
                waveOut.Stop();
                IsPlaying = false;
                mp3 = null;
                vorbis = null;
            }
            try
            {
                mp3 = new AudioFileReader(file);
                waveOut.Init(mp3);
                slider.Maximum = mp3.Length;
                TimeFile.Text = mp3.TotalTime.ToString(@"hh\:mm\:ss");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                try
                {
                    vorbis = new VorbisWaveReader(file);
                    waveOut.Init(vorbis);
                    slider.Maximum = vorbis.Length;
                    TimeFile.Text = vorbis.TotalTime.ToString(@"hh\:mm\:ss");
                }
                catch
                {
                    MessageBox.Show("Не поддерживаемый формат файла!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            Volumes.Value = waveOut.Volume * 100;
            AudioFile audio = new AudioFile(file);
            slider.IsEnabled = true;
            FileName.Text = Path.GetFileNameWithoutExtension(file);
            FileInfo.Text = Tags(audio, file);
            waveOut.Play();
            IsPlaying = true;
            await WhilePosition();
        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (IsReturned)
            {
                mp3.Position = 0;
                waveOut.Play();
                return;
            }
            if (waveOut.PlaybackState == PlaybackState.Stopped)
            {
                try
                {
                    ListMusic.SelectedIndex += 1;
                }
                catch { }
            }
        }

        private void ReturnedButton(object sender, RoutedEventArgs e)
        {
            if (IsReturned)
            {
                IsReturned = false;
                BrushColorBorder.BorderBrush = null;
            }
            else
            {
                Brush brush = new SolidColorBrush(SourceChord.FluentWPF.AccentColors.ImmersiveSystemAccent);
                IsReturned = true;
                BrushColorBorder.BorderBrush = brush;
            }
        }

        private void ListMusic_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((InfoMusic)ListMusic.SelectedItem != null)
            {
                InfoMusic music = (InfoMusic)ListMusic.SelectedItem;
                NextPlay(music.LinkFile);
            }
        }

        private void BackAudio(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ListMusic.SelectedIndex - 1 > 0)
                {
                    ListMusic.SelectedIndex -= 1;
                }
            }
            catch { }
        }

        private void NextAudio(object sender, RoutedEventArgs e)
        {
            try
            {
                ListMusic.SelectedIndex += 1;
            }
            catch { }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
    public class InfoMusic
    {
        public int ID { get; set; }
        public string TitleMusic { get; set; }
        public string LinkFile { get; set; }
        public string Duration { get; set; }
    }
}