using Color = System.Windows.Media.Color;
using DirectXTexWrapper;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using LibBundle.Records;
using LibBundle;
using LibDat2;
using LibGGPK2.Records;
using LibGGPK2;
//using MenuItem = Wpf.Ui.Controls.MenuItem;
using Microsoft.Win32;
using PixelFormat = System.Windows.Media.PixelFormat;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System;
using TreeViewItem = Wpf.Ui.Controls.TreeViewItem;
using System.Diagnostics;
using ICSharpCode.AvalonEdit.Rendering;

namespace VisualGGPK2
{
    public partial class MainWindow
    {
        public static string Version;
        public GGPKContainer ggpkContainer;
        public static readonly BitmapFrame IconDir = BitmapFrame.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("VisualGGPK2.Resources.dir.ico"));
        public static readonly BitmapFrame IconFile = BitmapFrame.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("VisualGGPK2.Resources.file.ico"));
        public static readonly ContextMenu TreeMenu = new();
        public static readonly Encoding Unicode = new UnicodeEncoding(false, true);
        public static readonly Encoding UTF8 = new UTF8Encoding(false, false);
        public HttpClient http;
        public readonly bool BundleMode;
        public readonly bool SteamMode;
        protected string FilePath;
        internal static byte SelectedVersion;
        private string filterText = "";
        private const int SettingsVersion = 1;
        private readonly string bin_path = AppDomain.CurrentDomain.BaseDirectory + @"\visual.bin";
        private string officialPath = Directory.Exists(@"C:\Program Files (x86)\Grinding Gear Games\Path of Exile") ? @"C:\Program Files (x86)\Grinding Gear Games\Path of Exile" : string.Empty;
        private string steamPath = GetSteamInstallPath();
        private string epicPath = Directory.Exists(@"C:\Program Files\Epic Games\PathOfExile\Bundles2") ? @"C:\Program Files\Epic Games\PathOfExile\Bundles2" : string.Empty;
        private string garenaPath = Directory.Exists(@"C:\Program Files (x86)\Garena\Games\32808") ? @"C:\Program Files (x86)\Garena\Games\32808" : string.Empty;
        private string tencentPath = string.Empty;
        private readonly string syntax_colors_CSharp = Path.Combine(Environment.CurrentDirectory, "C#.xaml");

        public MainWindow()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 1; i < args.Length; i++)
                switch (args[i].ToLower())
                {
                    case "-bundle":
                        BundleMode = true;
                        break;

                    case "-steam":
                        SteamMode = true;
                        break;

                    default:
                        if (FilePath == null && File.Exists(args[i]))
                            FilePath = args[i];
                        break;
                }
            if (BundleMode && SteamMode)
            {
                MessageBox.Show(this, "BundleMode and SteamMode cannot be both true", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
            InitializeComponent();
            SearchPanel.Install(TextViewContent);
            TextViewContent.Options.AllowScrollBelowDocument = true;
            var highlighting = TextViewContent.SyntaxHighlighting;
            if (highlighting == null) return;

            // Load color definitions from the XAML resource dictionary
            var colorDictionary = new ResourceDictionary
            {
                Source = new Uri(syntax_colors_CSharp, UriKind.RelativeOrAbsolute) // Provide the actual path
            };

            foreach (var colorKey in colorDictionary.Keys)
            {
                if (highlighting.GetNamedColor(colorKey.ToString()) is { } namedColor && colorDictionary[colorKey] is Color color)
                {
                    namedColor.Foreground = new SimpleHighlightingBrush(color);
                }
            }

            foreach (var color in highlighting.NamedHighlightingColors) {
                color.FontWeight = null;
            }
            TextViewContent.SyntaxHighlighting = null;
            TextViewContent.SyntaxHighlighting = highlighting;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            pRing.Visibility = Visibility.Visible;
        }

        private async Task<bool> OfficialLoaded()
        {
            var ofd = new OpenFileDialog
            {
                DefaultExt = "ggpk",
                FileName = "Content.ggpk",
                Filter = "GGPK File|*.ggpk",
                InitialDirectory = officialPath
            };
            if (ofd.ShowDialog() != true) return false;


            // Show the ProgressRing before starting the loading operation
            pRing.IsIndeterminate = true;
            pRing.Visibility = Visibility.Visible;

            try
            {
                // Load the GGPKContainer asynchronously
                ggpkContainer = await Task.Run(() => new GGPKContainer(ofd.FileName, false, false));
                // Save FilePath
                FilePath = ofd.FileName;
                Dispatcher.Invoke(SaveSettings);

                // Populate the TreeView
                Tree.Items.Clear();
                var root = CreateNode(ggpkContainer.rootDirectory);
                Tree.Items.Add(root);
                root.IsExpanded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Hide the ProgressRing after the loading operation is done
                pRing.IsIndeterminate = false;
                pRing.Visibility = Visibility.Hidden;
                tooltip.Visibility = Visibility.Hidden;
                copyright.Visibility = Visibility.Hidden;
            }

            return true;
        }

        private async Task<bool> SteamLoaded()
        {
            var ofd = new OpenFileDialog
            {
                DefaultExt = "bin",
                FileName = "_.index.bin",
                Filter = "bin file|*.bin",
                InitialDirectory = steamPath
            };
            if (ofd.ShowDialog() != true) return false;

            // Show the ProgressRing before starting the loading operation
            pRing.IsIndeterminate = true;
            pRing.Visibility = Visibility.Visible;

            try
            {
                // Load the GGPKContainer asynchronously
                ggpkContainer = await Task.Run(() => new GGPKContainer(ofd.FileName, false, true));
                // Save FilePath
                FilePath = ofd.FileName;
                Dispatcher.Invoke(SaveSettings);

                // Populate the TreeView
                Tree.Items.Clear();
                var root = CreateNode(ggpkContainer.rootDirectory);
                Tree.Items.Add(root);
                root.IsExpanded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Hide the ProgressRing after the loading operation is done
                pRing.IsIndeterminate = false;
                pRing.Visibility = Visibility.Hidden;
                tooltip.Visibility = Visibility.Hidden;
                copyright.Visibility = Visibility.Hidden;
            }

            return true;
        }        

        private async Task<bool> GarenaLoaded()
        {
            var ofd = new OpenFileDialog
            {
                DefaultExt = "ggpk",
                FileName = "Content.ggpk",
                Filter = "GGPK File|*.ggpk",
                InitialDirectory = garenaPath
            };
            if (ofd.ShowDialog() != true) return false;

            // Show the ProgressRing before starting the loading operation
            pRing.IsIndeterminate = true;
            pRing.Visibility = Visibility.Visible;

            try
            {
                // Load the GGPKContainer asynchronously
                ggpkContainer = await Task.Run(() => new GGPKContainer(ofd.FileName));
                // Save FilePath
                FilePath = ofd.FileName;
                Dispatcher.Invoke(SaveSettings);

                // Populate the TreeView
                Tree.Items.Clear();
                var root = CreateNode(ggpkContainer.rootDirectory);
                Tree.Items.Add(root);
                root.IsExpanded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Hide the ProgressRing after the loading operation is done
                pRing.IsIndeterminate = false;
                pRing.Visibility = Visibility.Hidden;
                tooltip.Visibility = Visibility.Hidden;
                copyright.Visibility = Visibility.Hidden;
            }

            return true;
        }

        private async Task<bool> TencentLoaded()
        {
            var ofd = new OpenFileDialog
            {
                DefaultExt = "ggpk",
                FileName = "Content.ggpk",
                Filter = "GGPK File|*.ggpk",
                InitialDirectory = tencentPath
            };
            if (ofd.ShowDialog() != true) return false;
            // Show the ProgressRing before starting the loading operation
            pRing.IsIndeterminate = true;
            pRing.Visibility = Visibility.Visible;

            try
            {
                // Load the GGPKContainer asynchronously
                ggpkContainer = await Task.Run(() => new GGPKContainer(ofd.FileName));
                // Save FilePath
                FilePath = ofd.FileName;
                Dispatcher.Invoke(SaveSettings);

                // Populate the TreeView
                Tree.Items.Clear();
                var root = CreateNode(ggpkContainer.rootDirectory);
                Tree.Items.Add(root);
                root.IsExpanded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Hide the ProgressRing after the loading operation is done
                pRing.IsIndeterminate = false;
                pRing.Visibility = Visibility.Hidden;
                tooltip.Visibility = Visibility.Hidden;
                copyright.Visibility = Visibility.Hidden;
            }

            return true;
        }

        private static string GetSteamInstallPath()
        {
            const string defaultPath = @"C:\Program Files (x86)\Steam";
            var installPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Valve\Steam", "InstallPath", defaultPath);
            return installPath ?? defaultPath;
        }


        private void SaveSettings()
        {
            try
            {
                using var bw = new BinaryWriter(File.Create(bin_path));

                // String
                bw.Write(officialPath);
                bw.Write(steamPath);
                bw.Write(epicPath);
                bw.Write(garenaPath);
                bw.Write(tencentPath);
            }
            catch (Exception ex)
            {
                // ignored
#if DEBUG
				Debug.WriteLine(ex.Message);
#endif
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(bin_path)) return;

                using var br = new BinaryReader(File.OpenRead(bin_path));

                // String
                officialPath = br.ReadString();
                steamPath = br.ReadString();
                epicPath = br.ReadString();
                garenaPath = br.ReadString();
                tencentPath = br.ReadString();
            }
            catch (Exception ex)
            {
                // ignored
#if DEBUG
				Debug.WriteLine(ex.Message);
#endif
            }
        }

        /// <summary>
        /// Create a element of the TreeView
        /// </summary>
        public static TreeViewItem CreateNode(RecordTreeNode rtn)
        {
            var tvi = new TreeViewItem { Tag = rtn, Margin = new Thickness(0, 1, 0, 1) };
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            if (rtn is IFileRecord)
                stack.Children.Add(new System.Windows.Controls.Image // Icon
                {
                    Source = IconFile,
                    Width = 20,
                    Height = 20,
                    Margin = new Thickness(0, 0, 5, 0)
                });
            else
                stack.Children.Add(new System.Windows.Controls.Image // Icon
                {
                    Source = IconDir,
                    Width = 20,
                    Height = 20,
                    Margin = new Thickness(0, 0, 5, 0)
                });
            stack.Children.Add(new TextBlock { Text = rtn.Name }); // File/Directory Name
            tvi.Header = stack;
            if (rtn is not IFileRecord)
                tvi.Items.Add("Loading . . ."); // Add expand button
            tvi.ContextMenu = TreeMenu;
            return tvi;
        }

        /// <summary>
        /// Directory expanded event
        /// </summary>
        private void OnTreeExpanded(object sender, RoutedEventArgs e)
        {
            var tvi = e.Source as TreeViewItem;
            if (tvi.Items.Count == 1 && tvi.Items[0] is string) // Haven't been expanded yet
            {
                tvi.Items.Clear();
                foreach (var c in ((RecordTreeNode)tvi.Tag).Children)
                    tvi.Items.Add(CreateNode(c));
            }
        }

        /// <summary>
        /// TreeView selected changed event
        /// </summary>
        private void OnTreeSelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (Tree.SelectedItem is TreeViewItem tvi)
            {
                ImageView.Visibility = Visibility.Hidden;
                TextViewContent.Visibility = Visibility.Hidden;
                //OGGView.Visibility = Visibility.Hidden;
                DatView.Visibility = Visibility.Hidden;
                //BK2View.Visibility = Visibility.Hidden;
                //BANKView.Visibility = Visibility.Hidden;
                //ButtonSave.Visibility = Visibility.Hidden;
                if (tvi.Tag is RecordTreeNode rtn)
                {
                    TextBoxOffset.Text = "0x" + rtn.Offset.ToString("X");
                    TextBoxSize.Text = rtn.Length.ToString();
                    TextBoxHash.Text = rtn is LibGGPK2.Records.DirectoryRecord || rtn is LibGGPK2.Records.FileRecord ? BitConverter.ToString(rtn.Hash).Replace("-", "") : rtn is BundleFileNode bf ? bf.Hash.ToString("X") : ((BundleDirectoryNode)rtn).Hash.ToString("X");
                    TextBoxBundle.Text = "";
                    if (rtn is IFileRecord f)
                    {
                        if (f is LibGGPK2.Records.FileRecord fr) TextBoxSize.Text = fr.DataLength.ToString();
                        else TextBoxBundle.Text = ((BundleFileNode)f).BundleFileRecord.bundleRecord.Name;
                        switch (f.DataFormat)
                        {
                            case IFileRecord.DataFormats.Image:
                                var b = f.ReadFileContent(ggpkContainer.fileStream);
                                Image.Source = BitmapFrame.Create(new MemoryStream(b));
                                Image.Tag = b;
                                Image.Width = ImageView.ActualWidth;
                                Image.Height = ImageView.ActualHeight;
                                Canvas.SetLeft(Image, 0);
                                Canvas.SetTop(Image, 0);
                                ImageView.Visibility = Visibility.Visible;
                                break;

                            case IFileRecord.DataFormats.Ascii:
                                TextViewContent.IsReadOnly = false;
                                TextViewContent.Text = UTF8.GetString(f.ReadFileContent(ggpkContainer.fileStream));
                                TextViewContent.Visibility = Visibility.Visible;
                                ButtonSave.Visibility = Visibility.Visible;
                                break;

                            case IFileRecord.DataFormats.Unicode:
                                if (rtn.Parent.Name == "Bundles" || rtn.Name == "minimap_colours.txt")
                                    goto case IFileRecord.DataFormats.Ascii;
                                TextViewContent.IsReadOnly = false;
                                TextViewContent.Text = Unicode.GetString(f.ReadFileContent(ggpkContainer.fileStream)).TrimStart('\xFEFF');
                                TextViewContent.Visibility = Visibility.Visible;
                                ButtonSave.Visibility = Visibility.Visible;
                                break;

                            case IFileRecord.DataFormats.OGG:
                                //TODO
                                //OGGView.Visibility = Visibility.Visible;
                                break;

                            case IFileRecord.DataFormats.Dat:
                                try
                                {
                                    DatView.Visibility = Visibility.Visible;
                                    ShowDatFile(new DatContainer(f.ReadFileContent(ggpkContainer.fileStream), rtn.Name, SchemaMin.IsChecked == true));
                                }
                                catch (Exception ex)
                                {
                                    toMark.Clear();
                                    DatTable.Tag = null;
                                    DatTable.Columns.Clear();
                                    DatTable.ItemsSource = null;
                                    DatReferenceDataTable.Columns.Clear();
                                    DatReferenceDataTable.ItemsSource = null;
                                    MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                break;

                            case IFileRecord.DataFormats.TextureDds:
                                try
                                {
                                    var buffer = f.ReadFileContent(ggpkContainer.fileStream);
                                    buffer = DdsRedirectAndHeaderProcess(buffer, rtn);
                                    Image.Source = DdsToBitmap(buffer);
                                    Image.Tag = buffer;
                                    Image.Width = ImageView.ActualWidth;
                                    Image.Height = ImageView.ActualHeight;
                                    Canvas.SetLeft(Image, 0);
                                    Canvas.SetTop(Image, 0);
                                    ImageView.Visibility = Visibility.Visible;
                                }
                                catch (Exception ex)
                                {
                                    TextViewContent.Text = ex.ToString();
                                    TextViewContent.IsReadOnly = true;
                                    TextViewContent.Visibility = Visibility.Visible;
                                }
                                break;

                            case IFileRecord.DataFormats.BK2:
                                //TODO
                                //BK2View.Visibility = Visibility.Visible;
                                break;

                            case IFileRecord.DataFormats.BANK:
                                //TODO
                                //BANKView.Visibility = Visibility.Visible;
                                break;
                        }
                    }
                }
            }
        }

        public static byte[] DdsRedirectAndHeaderProcess(byte[] buffer, RecordTreeNode node)
        {
            ReadOnlySpan<byte> span = buffer;
            if (node.Name.EndsWith(".header"))
                span = span[0] == 3 ? span[28..] : span[16..];
            while (span[0] == '*')
            {
                var path = UTF8.GetString(span[1..]);
                var f = (IFileRecord)node.ggpkContainer.FindRecord(path, node.ggpkContainer.FakeBundles2);
                span = buffer = f.ReadFileContent(node.ggpkContainer.fileStream);
                if (path.EndsWith(".header"))
                    span = span[0] == 3 ? span[28..] : span[16..];
            }
            return buffer == span ? buffer : span.ToArray();
        }

        public static DXGI_FORMAT_Managed ToWPFSupported(DXGI_FORMAT_Managed format)
        {
            switch (format)
            {
                case DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8A8_UNORM:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8A8_TYPELESS:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8X8_UNORM:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8X8_TYPELESS:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_B5G5R5A1_UNORM:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_B5G6R5_UNORM:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R1_UNORM:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R32_FLOAT:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R32_TYPELESS:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_D32_FLOAT:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R16_UNORM:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R16_UINT:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R16_TYPELESS:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_D16_UNORM:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R8_UNORM:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R8_UINT:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R8_TYPELESS:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_A8_UNORM:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16B16A16_UNORM:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16B16A16_UINT:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16B16A16_TYPELESS:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R32G32B32A32_FLOAT:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R32G32B32A32_TYPELESS:
                    return format;

                case DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16_UNORM:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16_UINT:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16_FLOAT:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16_SNORM:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16_SINT:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R10G10B10A2_UNORM:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R10G10B10A2_UINT:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R10G10B10A2_TYPELESS:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM:
                    return DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16B16A16_UNORM;

                case DXGI_FORMAT_Managed.DXGI_FORMAT_R9G9B9E5_SHAREDEXP:
                    return DXGI_FORMAT_Managed.DXGI_FORMAT_R10G10B10A2_UNORM;

                case DXGI_FORMAT_Managed.DXGI_FORMAT_R24_UNORM_X8_TYPELESS:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_D24_UNORM_S8_UINT:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_D32_FLOAT_S8X24_UINT:
                    return DXGI_FORMAT_Managed.DXGI_FORMAT_R32_FLOAT;

                case DXGI_FORMAT_Managed.DXGI_FORMAT_R16_FLOAT:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R16_SNORM:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_R16_SINT:
                    return DXGI_FORMAT_Managed.DXGI_FORMAT_R16_UNORM;

                case DXGI_FORMAT_Managed.DXGI_FORMAT_X24_TYPELESS_G8_UINT:
                case DXGI_FORMAT_Managed.DXGI_FORMAT_X32_TYPELESS_G8X24_UINT:
                    return DXGI_FORMAT_Managed.DXGI_FORMAT_R8_UNORM;

                default:
                    var bpp = DirectXTex.BitsPerPixel(format);
                    if (bpp <= 32)
                        return DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8A8_UNORM;
                    if (bpp <= 64)
                        return DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16B16A16_UNORM;
                    return DXGI_FORMAT_Managed.DXGI_FORMAT_R32G32B32A32_FLOAT;
            };
        }

        public static PixelFormat? ToPixelFormat(DXGI_FORMAT_Managed format, bool isPMAlpha)
        {
            if (isPMAlpha)
                return format switch
                {
                    DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16B16A16_UNORM or
                    DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16B16A16_UINT or
                    DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16B16A16_TYPELESS => PixelFormats.Prgba64,
                    DXGI_FORMAT_Managed.DXGI_FORMAT_R32G32B32A32_FLOAT or
                    DXGI_FORMAT_Managed.DXGI_FORMAT_R32G32B32A32_TYPELESS => PixelFormats.Prgba128Float,
                    _ => null
                };
            return format switch
            {
                DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8A8_UNORM or
                DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8A8_TYPELESS or
                DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8A8_UNORM_SRGB => PixelFormats.Bgra32,
                DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8X8_UNORM or
                DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8X8_TYPELESS or
                DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8X8_UNORM_SRGB => PixelFormats.Bgr32,
                DXGI_FORMAT_Managed.DXGI_FORMAT_B5G5R5A1_UNORM => PixelFormats.Bgr555,
                DXGI_FORMAT_Managed.DXGI_FORMAT_B5G6R5_UNORM => PixelFormats.Bgr565,
                DXGI_FORMAT_Managed.DXGI_FORMAT_R1_UNORM => PixelFormats.BlackWhite,
                DXGI_FORMAT_Managed.DXGI_FORMAT_R32_FLOAT or
                DXGI_FORMAT_Managed.DXGI_FORMAT_R32_TYPELESS or
                DXGI_FORMAT_Managed.DXGI_FORMAT_D32_FLOAT => PixelFormats.Gray32Float,
                DXGI_FORMAT_Managed.DXGI_FORMAT_R16_UNORM or
                DXGI_FORMAT_Managed.DXGI_FORMAT_R16_UINT or
                DXGI_FORMAT_Managed.DXGI_FORMAT_R16_TYPELESS or
                DXGI_FORMAT_Managed.DXGI_FORMAT_D16_UNORM => PixelFormats.Gray16,
                DXGI_FORMAT_Managed.DXGI_FORMAT_R8_UNORM or
                DXGI_FORMAT_Managed.DXGI_FORMAT_R8_UINT or
                DXGI_FORMAT_Managed.DXGI_FORMAT_R8_TYPELESS or
                DXGI_FORMAT_Managed.DXGI_FORMAT_A8_UNORM => PixelFormats.Gray8,
                DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16B16A16_UNORM or
                DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16B16A16_UINT or
                DXGI_FORMAT_Managed.DXGI_FORMAT_R16G16B16A16_TYPELESS => PixelFormats.Rgba64,
                DXGI_FORMAT_Managed.DXGI_FORMAT_R32G32B32A32_FLOAT or
                DXGI_FORMAT_Managed.DXGI_FORMAT_R32G32B32A32_TYPELESS => PixelFormats.Rgba128Float,
                _ => null
            };
        }

        public static unsafe BitmapSource DdsToBitmap(byte[] buffer)
        {
            fixed (byte* p = buffer)
                if (*(int*)p != 0x20534444) // "DDS "
                    buffer = BrotliSharpLib.Brotli.DecompressBuffer(buffer, 4, buffer.Length - 4); // for game before v3.11.2
            var image = new DirectXTexWrapper.Image();
            try
            {
                fixed (byte* p = buffer)
                {
                    var hr = DirectXTex.LoadDDSSingleFrame(p, buffer.Length, ref image);
                    if (hr < 0)
                        throw new COMException("Failed to read dds file", hr);
                }

                var pma = image.IsPMAlpha();
                var format = ToPixelFormat(image.format, pma);
                if (!format.HasValue)
                {
                    var newFormat = ToWPFSupported(image.format);
                    var hr = DirectXTex.Convert(ref image, newFormat);
                    if (hr < 0)
                        throw new COMException($"Failed to convert dds image format from {image.format} to {newFormat}", hr);
                    format = ToPixelFormat(image.format, pma);
                }
                return BitmapSource.Create(image.width, image.height, 96, 96, format.Value, null, image.pixels, image.slicePitch, image.rowPitch);
            }
            finally
            {
                image.Release();
            }
        }

        [DllImport("mfplat")]
        private static extern DXGI_FORMAT_Managed MFMapDX9FormatToDXGIFormat(uint dx9);

        public static BLOB BitmapToDdsFile(Bitmap bitmap)
        {
            var format = bitmap.PixelFormat;
            var dformat = DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8A8_UNORM;
            switch (format)
            {
                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                    dformat = DXGI_FORMAT_Managed.DXGI_FORMAT_B8G8R8X8_UNORM;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format16bppRgb565:
                    dformat = DXGI_FORMAT_Managed.DXGI_FORMAT_B5G6R5_UNORM;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format16bppRgb555:
                    format = System.Drawing.Imaging.PixelFormat.Format16bppRgb565;
                    dformat = DXGI_FORMAT_Managed.DXGI_FORMAT_B5G6R5_UNORM;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale:
                    dformat = DXGI_FORMAT_Managed.DXGI_FORMAT_R16_UNORM;
                    break;

                default:
                    format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
                    break;
            }

            var bd = bitmap.LockBits(new(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, format);
            var image = new DirectXTexWrapper.Image
            {
                width = bd.Width,
                height = bd.Height,
                format = dformat,
                pixels = bd.Scan0,
                rowPitch = bd.Stride,
                slicePitch = bd.Stride * bd.Height
            };
            DirectXTex.SaveDds(ref image, out var blob);
            image.Release();
            bitmap.UnlockBits(bd);
            return blob;
        }

        /// <summary>
        /// TreeViewItem MouseDown event
        /// </summary>
        private void OnTreePreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is not DependencyObject ui || ui is TreeView) return;

            // Get Clicked TreeViewItem
            while (ui is not TreeViewItem)
                if (ui != null) ui = VisualTreeHelper.GetParent(ui);
            var tvi = ui as TreeViewItem;

            if (e.ChangedButton != MouseButton.Left)
                tvi.IsSelected = true; // Select when clicked
            else if (tvi.Tag is LibGGPK2.Records.DirectoryRecord or BundleDirectoryNode && e.Source is not TreeViewItem) // Is Directory
                tvi.IsExpanded = true; // Expand when left clicked (but not on arrow)
        }

        private void OnDragEnter(object sender, DragEventArgs e) {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            if (!e.Effects.HasFlag(DragDropEffects.Copy)) return; // Drop File/Folder
            Activate();
            var dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
            string fileName;
            if (dropped.Length != 1 || (fileName = Path.GetFileName(dropped[0])) != ggpkContainer.rootDirectory.Name && !fileName.EndsWith(".zip"))
            {
                MessageBox.Show(this, "You can only drop root folder or a .zip file that contains it", "Replace Faild",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var bkg = new BackgroundDialog();
            if (fileName.EndsWith(".zip"))
                Task.Run(() =>
                {
                    try
                    {
                        var f = ZipFile.OpenRead(dropped[0]);
                        var es = f.Entries;
                        var list = new List<KeyValuePair<IFileRecord, ZipArchiveEntry>>(es.Count);
                        try
                        {
                            ggpkContainer.GetFileListFromZip(es, list);
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                bkg.Close();
                            });
                            return;
                        }
                        var notOk = false;
                        Dispatcher.Invoke(() =>
                        {
                            if (notOk = MessageBox.Show(this, $"Replace {list.Count} Files?", "Replace Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                                bkg.Close();
                        });
                        if (notOk)
                        {
                            return;
                        }
                        bkg.ProgressText = "Replacing {0}/" + list.Count.ToString() + " Files . . .";
                        ggpkContainer.Replace(list, bkg.NextProgress);
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(this, "Replaced " + list.Count.ToString() + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                            bkg.Close();
                        });
                    }
                    catch (Exception ex)
                    {
                        App.HandleException(ex);
                        Dispatcher.Invoke(bkg.Close);
                    }
                });
            else
                Task.Run(() =>
                {
                    try
                    {
                        var list = new Collection<KeyValuePair<IFileRecord, string>>();
                        ggpkContainer.GetFileList(dropped[0], list);
                        var notOk = false;
                        Dispatcher.Invoke(() =>
                        {
                            if (notOk = MessageBox.Show(this, $"Replace {list.Count} Files?", "Replace Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                                bkg.Close();
                        });
                        if (notOk)
                        {
                            return;
                        }
                        bkg.ProgressText = "Replacing {0}/" + list.Count.ToString() + " Files . . .";
                        ggpkContainer.Replace(list, bkg.NextProgress);
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(this, "Replaced " + list.Count.ToString() + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                            bkg.Close();
                        });
                    }
                    catch (Exception ex)
                    {
                        App.HandleException(ex);
                        Dispatcher.Invoke(bkg.Close);
                    }
                });
            bkg.ShowDialog();
        }

        private void OnExportClicked(object sender, RoutedEventArgs e)
        {
            if (Tree.SelectedItem is TreeViewItem { Tag: RecordTreeNode rtn })
            {
                var sfd = new SaveFileDialog();
                if (rtn is IFileRecord fr)
                {
                    sfd.FileName = rtn.Name;
                    if (sfd.ShowDialog() == true)
                    {
                        File.WriteAllBytes(sfd.FileName, fr.ReadFileContent(ggpkContainer.fileStream));
                        MessageBox.Show(this, "Exported " + rtn.GetPath() + "\nto " + sfd.FileName, "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    sfd.FileName = rtn.Name + ".dir";
                    if (sfd.ShowDialog() == true)
                    {
                        var bkg = new BackgroundDialog();
                        Task.Run(() =>
                        {
                            try
                            {
                                var list = new List<KeyValuePair<IFileRecord, string>>();
                                var path = Path.GetDirectoryName(sfd.FileName) + "\\" + rtn.Name;
                                GGPKContainer.RecursiveFileList(rtn, path, list, true);
                                bkg.ProgressText = "Exporting {0}/" + list.Count.ToString() + " Files . . .";
                                list.Sort((a, b) => BundleSortComparer.Instance.Compare(a.Key, b.Key));
                                var failFileCount = 0;
                                try
                                {
                                    GGPKContainer.Export(list, bkg.NextProgress);
                                }
                                catch (GGPKContainer.BundleMissingException bex)
                                {
                                    failFileCount = bex.failFiles;
                                    Dispatcher.Invoke(() => MessageBox.Show(this, bex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning));
                                }
                                Dispatcher.Invoke(() =>
                                {
                                    MessageBox.Show(this, "Exported " + (list.Count - failFileCount).ToString() + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                                    bkg.Close();
                                });
                            }
                            catch (Exception ex)
                            {
                                App.HandleException(ex);
                                Dispatcher.Invoke(bkg.Close);
                            }
                        });
                        bkg.ShowDialog();
                    }
                }
            }
        }

        private void OnReplaceClicked(object sender, RoutedEventArgs e)
        {
            if (Tree.SelectedItem is TreeViewItem { Tag: RecordTreeNode rtn })
            {
                if (rtn is IFileRecord fr)
                {
                    var ofd = new OpenFileDialog { FileName = rtn.Name };
                    if (ofd.ShowDialog() == true)
                    {
                        fr.ReplaceContent(File.ReadAllBytes(ofd.FileName));
                        MessageBox.Show(this, "Replaced " + rtn.GetPath() + "\nwith " + ofd.FileName, "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                        OnTreeSelectedChanged(null, null);
                    }
                }
                else
                {
                    var ofd = new OpenFolderDialog();
                    if (ofd.ShowDialog() == true)
                    {
                        var bkg = new BackgroundDialog();
                        Task.Run(() =>
                        {
                            try
                            {
                                var list = new Collection<KeyValuePair<IFileRecord, string>>();
                                GGPKContainer.RecursiveFileList(rtn, ofd.DirectoryPath, list, false);
                                bkg.ProgressText = "Replacing {0}/" + list.Count.ToString() + " Files . . .";
                                ggpkContainer.Replace(list, bkg.NextProgress);
                                Dispatcher.Invoke(() =>
                                {
                                    MessageBox.Show(this, "Replaced " + list.Count.ToString() + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                                    bkg.Close();
                                });
                            }
                            catch (Exception ex)
                            {
                                App.HandleException(ex);
                                Dispatcher.Invoke(bkg.Close);
                            }
                        });
                        bkg.ShowDialog();
                    }
                }
            }
        }

        private void OnSaveTextClicked(object sender, RoutedEventArgs e)
        {
            if (Tree.SelectedItem is TreeViewItem tvi && tvi.Tag is IFileRecord fr)
            {
                switch (fr.DataFormat)
                {
                    case IFileRecord.DataFormats.Ascii:
                        fr.ReplaceContent(UTF8.GetBytes(TextViewContent.Text));
                        break;

                    case IFileRecord.DataFormats.Unicode:
                        if (((RecordTreeNode)fr).GetPath().EndsWith(".amd"))
                            fr.ReplaceContent(Unicode.GetBytes(TextViewContent.Text));
                        else if (((RecordTreeNode)fr).Parent.Name == "Bundles" || ((RecordTreeNode)fr).Name == "minimap_colours.txt")
                            goto case IFileRecord.DataFormats.Ascii;
                        else
                            fr.ReplaceContent(Unicode.GetBytes("\xFEFF" + TextViewContent.Text));
                        break;

                    default:
                        return;
                }
                MessageBox.Show(this, "Saved to " + ((RecordTreeNode)fr).GetPath(), "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnRecoveryClicked(object sender, RoutedEventArgs e)
        {
            if (new VersionSelector().ShowDialog() != true) return;

            var bkg = new BackgroundDialog();
            var rtn = (RecordTreeNode)((TreeViewItem)Tree.SelectedItem).Tag;
            Task.Run(() =>
            {
                try
                {
                    string PatchServer = null;
                    var indexUrl = SelectedVersion switch
                    {
                        1 => (PatchServer = GetPatchServer()) + "Bundles2/_.index.bin",
                        2 => (PatchServer = GetPatchServer(true)) + "Bundles2/_.index.bin",
                        3 => "http://poesmoother.eu/nextcloud/index.php/s/8x8rz3zZgZfsTCz/download",
                        4 => (PatchServer = GetPatchServer()) + "Bundles2/_.index.bin",
                        _ => null
                    };

                    if (SelectedVersion == 4)
                    {
                        var outsideBundles2 = true;
                        var tmp = rtn;
                        do
                        {
                            if (tmp == ggpkContainer.FakeBundles2)
                                outsideBundles2 = false;
                            tmp = tmp.Parent;
                        } while (tmp != null);
                        if (outsideBundles2)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(this, "Tencent version currently only support recovering files under \"Bundles2\" directory!", "Unsupported", MessageBoxButton.OK, MessageBoxImage.Error);
                                bkg.Close();
                            });
                            return;
                        }
                    }

                    var l = new List<IFileRecord>();
                    GGPKContainer.RecursiveFileList(rtn, l);
                    bkg.ProgressText = "Recovering {0}/" + l.Count.ToString() + " Files . . .";

                    if (http == null)
                    {
                        http = new()
                        {
                            Timeout = Timeout.InfiniteTimeSpan
                        };
                        http.DefaultRequestHeaders.Add("User-Agent", "VisualGGPK2");
                    }

                    IndexContainer i = null;
                    if (l.Any((ifr) => ifr is BundleFileNode))
                    {
                        var br = new BinaryReader(new MemoryStream(http.GetByteArrayAsync(indexUrl).Result));
                        i = new IndexContainer(br);
                        br.Close();
                    }

                    foreach (var f in l)
                    {
                        if (f is BundleFileNode bfn)
                        {
                            var bfr = bfn.BundleFileRecord;
                            var newbfr = i.FindFiles[bfr.NameHash];
                            bfr.Offset = newbfr.Offset;
                            bfr.Size = newbfr.Size;
                            if (bfr.BundleIndex != newbfr.BundleIndex)
                            {
                                bfr.BundleIndex = newbfr.BundleIndex;
                                bfr.bundleRecord.Files.Remove(bfr);
                                bfr.bundleRecord = ggpkContainer.Index.Bundles[bfr.BundleIndex];
                                bfr.bundleRecord.Files.Add(bfr);
                            }
                        }
                        else
                        {
                            var fr = f as LibGGPK2.Records.FileRecord;
                            var path = Regex.Replace(fr.GetPath(), "^ROOT/", "");
                            fr.ReplaceContent(http.GetByteArrayAsync(PatchServer + path).Result);
                        }
                        bkg.NextProgress();
                    }

                    if (i != null)
                        if (SteamMode)
                            ggpkContainer.Index.Save("_.index.bin");
                        else
                            ggpkContainer.IndexRecord.ReplaceContent(ggpkContainer.Index.Save());
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(this, "Recoveried " + l.Count.ToString() + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                        bkg.Close();
                        OnTreeSelectedChanged(null, null);
                    });
                }
                catch (Exception ex)
                {
                    App.HandleException(ex);
                    Dispatcher.Invoke(bkg.Close);
                }
            });
            bkg.ShowDialog();
        }

        private static string GetPatchServer(bool garena = false)
        {
            var tcp = new TcpClient() { NoDelay = true };
            tcp.Connect(Dns.GetHostAddresses(garena ? "login.tw.pathofexile.com" : "us.login.pathofexile.com"), garena ? 12999 : 12995);
            var b = new byte[256];
            tcp.Client.Send(new byte[] { 1, 4 });
            tcp.Client.Receive(b);
            tcp.Close();
            return Encoding.Unicode.GetString(b, 35, b[34] * 2);
        }

        private void OnConvertPngClicked(object sender, RoutedEventArgs e)
        {
            if (Tree.SelectedItem is not TreeViewItem { Tag: RecordTreeNode rtn })
            {
                MessageBox.Show(this, "You must select a node first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (rtn is not IFileRecord)
            { // directory
                var sfd = new SaveFileDialog { FileName = rtn.Name + ".dir", Filter = "*.*|*.*" };
                if (sfd.ShowDialog() == true)
                {
                    var bkg = new BackgroundDialog();
                    Task.Run(() =>
                    {
                        try
                        {
                            var list = new List<KeyValuePair<IFileRecord, string>>();
                            var path = Path.GetDirectoryName(sfd.FileName) + "\\" + rtn.Name;
                            GGPKContainer.RecursiveFileList(rtn, path, list, true, ".dds$");
                            bkg.ProgressText = "Converting {0}/" + list.Count.ToString() + " Files . . .";
                            list.Sort((x, y) => BundleSortComparer.Instance.Compare(x.Key, y.Key));
                            var failFileCount = 0;
                            try
                            {
                                var fail = BatchConvertPng(list, bkg.NextProgress);
                                if (fail < 0)
                                {
                                    Dispatcher.Invoke(() => bkg.Close());
                                    return;
                                }
                                failFileCount += fail;
                            }
                            catch (GGPKContainer.BundleMissingException bex)
                            {
                                failFileCount = bex.failFiles;
                                Dispatcher.Invoke(() => MessageBox.Show(this, bex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning));
                            }
                            Dispatcher.Invoke(() =>
                            {
                                if (failFileCount > 0)
                                    MessageBox.Show(this, "Converted " + (list.Count - failFileCount) + " files\r\n" + failFileCount + "files failed", "Done", MessageBoxButton.OK, MessageBoxImage.Warning);
                                else
                                    MessageBox.Show(this, "Converted " + list.Count + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                                bkg.Close();
                            });
                        }
                        catch (Exception ex)
                        {
                            App.HandleException(ex);
                            Dispatcher.Invoke(bkg.Close);
                        }
                    });
                    bkg.ShowDialog();
                }
            }
            else
            { // file
                if (ImageView.Visibility != Visibility.Visible)
                    MessageBox.Show(this, "Selected file is not a dds file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                {
                    var buffer = (byte[])Image.Tag;

                    string name;
                    if (rtn.Name.EndsWith(".dds"))
                        name = rtn.Name[..^4] + ".png";
                    else if (rtn.Name.EndsWith(".dds.header"))
                        name = rtn.Name + ".png";
                    else
                    {
                        MessageBox.Show(this, "Selected file is not a dds file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var sfd = new SaveFileDialog { FileName = name, Filter = "*.png|*.png" };
                    if (sfd.ShowDialog() == true)
                    {
                        SaveImageSource(DdsToBitmap(buffer), sfd.FileName);
                        MessageBox.Show(this, "Saved " + sfd.FileName, "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void OnSavePngClicked(object sender, RoutedEventArgs e)
        {
            if (Tree.SelectedItem is not TreeViewItem { Tag: RecordTreeNode rtn })
            {
                MessageBox.Show(this, "You must select a image file first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var name = rtn.Name.EndsWith(".dds") ? rtn.Name[..^4] + ".png" : rtn.Name.EndsWith(".png") ? rtn.Name : rtn.Name + ".png";
            var sfd = new SaveFileDialog { FileName = name, Filter = "*.png|*.png" };
            if (sfd.ShowDialog() == true)
            {
                //DdsToPngFile(DdsToPng((byte[])Image.Tag), sfd.FileName);
                if (rtn.Name.EndsWith(".png"))
                    File.WriteAllBytes(sfd.FileName, (byte[])Image.Tag);
                else
                    SaveImageSource((BitmapSource)Image.Source, sfd.FileName);
                MessageBox.Show(this, "Saved " + sfd.FileName, "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public static void SaveImageSource(BitmapSource source, string path)
        {
            var fs = File.Create(path);
            var png = new PngBitmapEncoder();
            png.Frames.Add(source is BitmapFrame bf ? bf : BitmapFrame.Create(source));
            png.Save(fs);
            fs.Close();
        }

        private int BatchConvertPng(ICollection<KeyValuePair<IFileRecord, string>> list, Action ProgressStep = null)
        {
            var regex = new Regex(".dds$");
            LibBundle.Records.BundleRecord br = null;
            MemoryStream ms = null;
            var failBundles = 0;
            var failFiles = 0;
            var fail = 0;
            var done = 0;
            var semaphore = new Semaphore(Environment.ProcessorCount - 1, Environment.ProcessorCount - 1);
            COMException COM = null;
            string errorPath = null;

            foreach (var (record, path) in list)
            {
                if (COM != null)
                {
                    if (MessageBox.Show(this, $"Error when processing file: {errorPath}\r\n\r\n{COM}\r\nIgnore and continue?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Error) != MessageBoxResult.Yes)
                    {
                        semaphore.Dispose();
                        semaphore = null;
                        return -1;
                    }
                    COM = null;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (record is BundleFileNode bfn)
                {
                    if (br != bfn.BundleFileRecord.bundleRecord)
                    {
                        ms?.Close();
                        br = bfn.BundleFileRecord.bundleRecord;
                        br.Read(bfn.ggpkContainer.Reader, bfn.ggpkContainer.RecordOfBundle(br)?.DataBegin);
                        ms = br.Bundle?.Read(bfn.ggpkContainer.Reader);
                        if (ms == null)
                            ++failBundles;
                    }
                    if (ms == null)
                        ++failFiles;
                    else
                    {
                        if (COM != null)
                        {
                            if (MessageBox.Show(this, $"Error when processing file: {errorPath}\r\n\r\n{COM}\r\nIgnore and continue?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Error) != MessageBoxResult.Yes)
                            {
                                semaphore.Dispose();
                                semaphore = null;
                                return -1;
                            }
                            COM = null;
                        }
                        semaphore.WaitOne();
                        var b = bfn.BatchReadFileContent(ms);
                        Task.Run(() =>
                        {
                            try
                            {
                                b = DdsRedirectAndHeaderProcess(b, bfn);
                                SaveImageSource(DdsToBitmap(b), path[..^4] + ".png");
                                Interlocked.Increment(ref done);
                                ProgressStep?.Invoke();
                                semaphore?.Release();
                            }
                            catch (COMException ex)
                            {
                                Interlocked.Increment(ref fail);
                                if (COM == null)
                                {
                                    COM = ex;
                                    errorPath = bfn.GetPath();
                                    semaphore?.Release();
                                }
                            }
                        });
                    }
                }
                else
                {
                    if (COM != null)
                    {
                        if (MessageBox.Show(this, $"Error when processing file: {errorPath}\r\n\r\n{COM}\r\nIgnore and continue?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Error) != MessageBoxResult.Yes)
                        {
                            semaphore.Dispose();
                            semaphore = null;
                            return -1;
                        }
                        COM = null;
                    }
                    semaphore.WaitOne();
                    var b = record.ReadFileContent();
                    Task.Run(() =>
                    {
                        try
                        {
                            b = DdsRedirectAndHeaderProcess(b, (RecordTreeNode)record);
                            SaveImageSource(DdsToBitmap(b), path[..^4] + ".png");
                            Interlocked.Increment(ref done);
                            ProgressStep?.Invoke();
                            if (COM == null)
                                semaphore.Release();
                        }
                        catch (COMException ex)
                        {
                            Interlocked.Increment(ref fail);
                            if (COM == null)
                            {
                                COM = ex;
                                errorPath = ((RecordTreeNode)record).GetPath();
                                semaphore?.Release();
                            }
                        }
                    });
                }
            }
            semaphore.WaitOne();
            while (done < list.Count)
                Thread.Sleep(500);
            semaphore.Dispose();
            if (failBundles != 0 || failFiles != 0)
                throw new GGPKContainer.BundleMissingException(failBundles, failFiles);
            return fail;
        }

        private unsafe void OnWriteImageClicked(object sender, RoutedEventArgs e)
        {
            if (Tree.SelectedItem is not TreeViewItem { Tag: RecordTreeNode rtn })
            {
                MessageBox.Show(this, "You must select a node first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (rtn is IFileRecord fr)
            {
                if (ImageView.Visibility != Visibility.Visible || !rtn.Name.EndsWith(".dds"))
                {
                    MessageBox.Show(this, "Selected file is not a dds file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var ofd = new OpenFileDialog { FileName = rtn.Name, Filter = "Image File|*.png;*.jpg;*.bmp;*.jpeg;*.gif;*.tiff;*.ico|*.*|*.*" };
                if (ofd.ShowDialog() != true)
                    return;
                if (MessageBox.Show(this, "The image will directly write into the dds in ggpk.\r\nThis is an experimental feature and may not work as expected!\r\n\r\nClick OK to continue", "Error", MessageBoxButton.OK, MessageBoxImage.Warning) != MessageBoxResult.OK)
                    return;

                var bitmap = (Bitmap)System.Drawing.Image.FromFile(ofd.FileName);
                /*uint fourcc; // Get origin dds DXGI_FORMAT
				fixed (byte* p = (byte[])Image.Tag)
					fourcc = *(uint*)(p + 84); */
                using (var blob = BitmapToDdsFile(bitmap))
                    fr.ReplaceContent(new ReadOnlySpan<byte>(blob.Pointer, blob.Length).ToArray());

                MessageBox.Show(this, "Wrote " + ofd.FileName + "\r\ninto " + rtn.GetPath(), "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                OnTreeSelectedChanged(null, null);
            }
            else
            { // directory
                var ofd = new OpenFolderDialog();
                if (ofd.ShowDialog() == true)
                {
                    var bkg = new BackgroundDialog();
                    Task.Run(() =>
                    {
                        try
                        {
                            var list = new List<KeyValuePair<IFileRecord, string>>();
                            void Recursive(RecordTreeNode record, string path)
                            {
                                if (record is IFileRecord fr)
                                {
                                    path = path[..^4] + ".png";
                                    if (record.Name.EndsWith(".dds") && File.Exists(path))
                                        list.Add(new(fr, path));
                                }
                                else
                                    foreach (var f in record.Children)
                                    {
                                        var i = f.Name.LastIndexOf('.');
                                        Recursive(f, path + "/" + f.Name);
                                    }
                            }
                            Recursive(rtn, ofd.DirectoryPath);
                            bkg.ProgressText = "Writing {0}/" + list.Count.ToString() + " image files . . .";

                            var bundles = ggpkContainer.Index == null ? new() : new List<LibBundle.Records.BundleRecord>(ggpkContainer.Index.Bundles);
                            var changed = false;
                            var BundleToSave = ggpkContainer.Index?.GetSmallestBundle(bundles);
                            var fr = BundleToSave == null ? null : ggpkContainer.RecordOfBundle(BundleToSave);

                            if (ggpkContainer.Index != null) // else BundleMode
                                if (ggpkContainer.fileStream == null) // SteamMode
                                    while (!File.Exists(BundleToSave.Name))
                                    {
                                        bundles.Remove(BundleToSave);
                                        if (bundles.Count == 0)
                                            throw new("Couldn't find a bundle to save");
                                        BundleToSave = ggpkContainer.Index.GetSmallestBundle(bundles);
                                    }
                                else
                                    while (fr == null)
                                    {
                                        bundles.Remove(BundleToSave);
                                        if (bundles.Count == 0)
                                            throw new("Couldn't find a bundle to save");
                                        BundleToSave = ggpkContainer.Index.GetSmallestBundle(bundles);
                                        fr = ggpkContainer.RecordOfBundle(BundleToSave);
                                    }

                            var SavedSize = 0;
                            Parallel.ForEach(list, (kv) =>
                            {
                                var bitmap = (Bitmap)System.Drawing.Image.FromFile(kv.Value);
                                /*uint fourcc; // Get origin dds DXGI_FORMAT
								fixed (byte* p = (byte[])Image.Tag)
									fourcc = *(uint*)(p + 84); */
                                using var blob = BitmapToDdsFile(bitmap);
                                var b = new ReadOnlySpan<byte>(blob.Pointer, blob.Length).ToArray();

                                lock (ggpkContainer)
                                {
                                    if (SavedSize > 200000000 && bundles.Count > 1)
                                    { // 200MB per bundle
                                        changed = true;
                                        if (ggpkContainer.fileStream == null) // SteamMode
                                            BundleToSave.Save();
                                        else
                                        {
                                            fr.ReplaceContent(BundleToSave.Save(ggpkContainer.Reader, fr.DataBegin));
                                            BundleToSave.Bundle.offset = fr.DataBegin;
                                            BundleFileNode.LastFileToUpdate.RemoveOldCache(BundleToSave);
                                        }
                                        BundleToSave = ggpkContainer.Index.GetSmallestBundle();

                                        if (ggpkContainer.Index != null)
                                        { // else BundleMode
                                            fr = ggpkContainer.RecordOfBundle(BundleToSave);
                                            if (ggpkContainer.fileStream == null) // SteamMode
                                                while (!File.Exists(BundleToSave.Name))
                                                {
                                                    bundles.Remove(BundleToSave);
                                                    BundleToSave = ggpkContainer.Index.GetSmallestBundle(bundles);
                                                }
                                            else
                                                while (fr == null)
                                                {
                                                    bundles.Remove(BundleToSave);
                                                    BundleToSave = ggpkContainer.Index.GetSmallestBundle(bundles);
                                                    fr = ggpkContainer.RecordOfBundle(BundleToSave);
                                                }
                                        }
                                        SavedSize = 0;
                                    }

                                    if (kv.Key is BundleFileNode bfn) // In Bundle
                                        SavedSize += bfn.BatchReplaceContent(b, BundleToSave);
                                    else // In GGPK
                                        kv.Key.ReplaceContent(b);
                                }
                                bkg.NextProgress();
                            });
                            if (BundleToSave != null && SavedSize > 0)
                            {
                                changed = true;
                                if (ggpkContainer.fileStream == null) // SteamMode
                                    BundleToSave.Save();
                                else
                                {
                                    fr.ReplaceContent(BundleToSave.Save(ggpkContainer.Reader, fr.DataBegin));
                                    BundleToSave.Bundle.offset = fr.DataBegin;
                                    BundleFileNode.LastFileToUpdate.RemoveOldCache(BundleToSave);
                                }
                            }
                            // Save Index
                            if (changed)
                                if (fr == null)
                                    ggpkContainer.Index.Save("_.index.bin");
                                else
                                    ggpkContainer.IndexRecord.ReplaceContent(ggpkContainer.Index.Save());

                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(this, "Wrote " + list.Count.ToString() + " Files", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                                OnTreeSelectedChanged(null, null);
                                bkg.Close();
                            });
                        }
                        catch (Exception ex)
                        {
                            App.HandleException(ex);
                            Dispatcher.Invoke(bkg.Close);
                        }
                    });
                    bkg.ShowDialog();
                }
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            if (ggpkContainer == null)
            {
                MessageBox.Show("GGPK is not loaded!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Filtering
            Tree.Items.Clear();
            ggpkContainer.FakeBundles2.Children.Clear();
            foreach (var f in ggpkContainer.Index.Files)
                if (RegexCheckBox.IsChecked != null && (RegexCheckBox.IsChecked.Value && Regex.IsMatch(f.path, filterText) ||
                                                    !RegexCheckBox.IsChecked.Value && f.path.Contains(filterText)))
                    ggpkContainer.BuildBundleTree(f, ggpkContainer.FakeBundles2);
            var root = CreateNode(ggpkContainer.rootDirectory);
            Tree.Items.Add(root);
            root.IsExpanded = true;

            var l = new SortedSet<IFileRecord>(BundleSortComparer.Instance);
            GGPKContainer.RecursiveFileList(ggpkContainer.FakeBundles2, l);
            if (MessageBox.Show($"Are you going to search in {l.Count} files?", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
            Tree.Items.Clear();
            ggpkContainer.FakeBundles2.Children.Clear();

            var bkg = new BackgroundDialog { ProgressText = "Searching in {0}/" + l.Count + " Files . . ." };
            var toSearch = SearchBox.Text;
            Task.Run(() =>
            {
                try
                {
                    BundleRecord br = null;
                    MemoryStream ms = null;
                    foreach (var f in l.Cast<BundleFileNode>())
                    {
                        if (br != f.BundleFileRecord.bundleRecord)
                        {
                            ms?.Close();
                            br = f.BundleFileRecord.bundleRecord;
                            br.Read(f.ggpkContainer.Reader, f.ggpkContainer.RecordOfBundle(br)?.DataBegin);
                            ms = br.Bundle.Read(f.ggpkContainer.Reader);
                        }
                        var b = f.BatchReadFileContent(ms);
                        var s = f.DataFormat switch
                        {
                            IFileRecord.DataFormats.Ascii => UTF8.GetString(b),
                            IFileRecord.DataFormats.Unicode => Unicode.GetString(b)/*.TrimStart('\xFEFF')*/, //Not necessary for search
                            _ => null
                        };
                        if (s?.Contains(toSearch) == true)
                            ggpkContainer.BuildBundleTree(f.BundleFileRecord, ggpkContainer.FakeBundles2);
                        bkg.NextProgress();
                    }

                    Dispatcher.Invoke(() =>
                    {
                        var node = CreateNode(ggpkContainer.rootDirectory);
                        Tree.Items.Add(node);
                        node.IsExpanded = true;
                        //ggpkContainer.fileStream?.Close();
                        bkg.Close();
                    });
                }
                catch (Exception ex)
                {
                    App.HandleException(ex);
                }
            });
            bkg.ShowDialog();
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (ggpkContainer == null) { MessageBox.Show("GGPK is not loaded.!", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            Tree.Items.Clear();
            ggpkContainer.FakeBundles2.Children.Clear();
            foreach (var f in ggpkContainer.Index.Files)
                if (RegexCheckBox.IsChecked != null && (RegexCheckBox.IsChecked.Value && Regex.IsMatch(f.path, filterText = FilterBox.Text) ||
                                                    !RegexCheckBox.IsChecked.Value && f.path.Contains(filterText = FilterBox.Text)))
                    ggpkContainer.BuildBundleTree(f, ggpkContainer.FakeBundles2);
            var root = CreateNode(ggpkContainer.rootDirectory);
            Tree.Items.Add(root);
            root.IsExpanded = true;
        }

        private void FilterBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            FilterButton_Click(null, null);
            e.Handled = true;
        }

        private void AllowGameOpen_Click(object sender, RoutedEventArgs e)
        {
            ggpkContainer.fileStream.Close();
            FileInfo fi = new(FilePath);
            var t = fi.LastWriteTimeUtc;
            var l = fi.Length;
            string dir = Path.GetDirectoryName(FilePath);
            Process.Start(new ProcessStartInfo(dir + @"\PathOfExile_x64.exe")
            {
                WorkingDirectory = dir
            });

            bool working = true;

        loop:
            try
            {
                if (!working) return;
                Task.Delay(1000).Wait();

                fi = new FileInfo(FilePath);
                if (fi.LastWriteTimeUtc != t || fi.Length != l)
                {
                    Tree.Items.Clear();
                    TextViewContent.Visibility = Visibility.Visible;
                    FilterButton.IsEnabled = false;
                    AllowGameOpen.IsEnabled = false;
                }
                else
                {
                    ggpkContainer.fileStream = File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                    ggpkContainer.Reader = new BinaryReader(ggpkContainer.fileStream);
                    ggpkContainer.Writer = new BinaryWriter(ggpkContainer.fileStream);
                }
            }
            catch (IOException)
            {
                goto loop;
            }
        }

        //private async void AllowGameOpen_Click(object sender, RoutedEventArgs e)
        //{
        //    ggpkContainer.fileStream.Close();
        //    var fi = new FileInfo(FilePath);
        //    var t = fi.LastWriteTimeUtc;
        //    var l = fi.Length;
        //    var dir = Path.GetDirectoryName(FilePath);
        //    Process.Start(new ProcessStartInfo(dir + @"\PathOfExile_x64.exe")
        //    {
        //        WorkingDirectory = dir
        //    });

        //loop:
        //    try
        //    {
        //        var uiMessageBox = new Wpf.Ui.Controls.MessageBox
        //        {
        //            Width = 140,
        //            Height = 40,
        //            Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x24, 0x28)),
        //            Title = "game mode   ",
        //            //Content = "Close this to enter in Edit Mode again!"
        //        };
        //        await uiMessageBox.ShowDialogAsync();

        //        fi = new FileInfo(FilePath);
        //        if (fi.LastWriteTimeUtc != t || fi.Length != l)
        //        {
        //            //MessageBox.Show(this, "The Content.ggpk has been modified, Now it's going to be reloaded", "GGPK Changed", MessageBoxButton.OK, MessageBoxImage.Warning);

        //            Tree.Items.Clear();
        //            //TextViewContent.Text = "Loading . . .";
        //            TextViewContent.Visibility = Visibility.Visible;
        //            FilterButton.IsEnabled = false;
        //            AllowGameOpen.IsEnabled = false;

        //            // Initial GGPK
        //            await Task.Run(() => ggpkContainer = new GGPKContainer(FilePath, BundleMode, SteamMode));

        //            var root = CreateNode(ggpkContainer.rootDirectory);
        //            Tree.Items.Add(root); // Initial TreeView
        //            root.IsExpanded = true;

        //            FilterButton.IsEnabled = true;
        //            if (!SteamMode) AllowGameOpen.IsEnabled = true;

        //            TextViewContent.AppendText("\r\n Done!");
        //        }
        //        else
        //        {
        //            ggpkContainer.fileStream = File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
        //            ggpkContainer.Reader = new BinaryReader(ggpkContainer.fileStream);
        //            ggpkContainer.Writer = new BinaryWriter(ggpkContainer.fileStream);
        //        }
        //    }
        //    catch (IOException)
        //    {
        //        MessageBox.Show(this, "Close the game first!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        goto loop;
        //    }
        //}

        public Task RestoreIndex()
        {
            try
            {
                http = new HttpClient();
                var download = GetPatchServer() + "Bundles2/_.index.bin";
                var index = http.GetByteArrayAsync(download).Result;
                ggpkContainer.IndexRecord.ReplaceContent(index);
                ggpkContainer.fileStream.Seek(ggpkContainer.IndexRecord.DataBegin, SeekOrigin.Begin);
                ggpkContainer.Index = new IndexContainer(ggpkContainer.Reader);
                ggpkContainer._RecordOfBundle.Clear();
            }
            catch (Exception ex)
            {
                var ew = new ErrorWindow();
                ew.ShowError(ex);
                if (ew.ShowDialog() != true)
                {
                    Application.Current.Shutdown();
                }
            }

            return Task.CompletedTask;
        }

        private async Task Restore_Official(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Restore original Content.ggpk?", "Confirm", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
            await RestoreIndex();
            MessageBox.Show("Successfully restored!", "Done");
        }

        private async void Restore_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(async () => {
                await Dispatcher.Invoke(async () => {
                    await Restore_Official(sender, e);
                });
            });
        }

        private void ImageView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var p = e.GetPosition(ImageView);
            var x = Canvas.GetLeft(Image);
            var y = Canvas.GetTop(Image);
            if (e.Delta > 0)
            {
                Canvas.SetLeft(Image, x - (p.X - x) * 0.2);
                Canvas.SetTop(Image, y - (p.Y - y) * 0.2);
                Image.Width *= 1.2;
                Image.Height *= 1.2;
            }
            else
            {
                Canvas.SetLeft(Image, x + (p.X - x) / 6);
                Canvas.SetTop(Image, y + (p.Y - y) / 6);
                Image.Width /= 1.2;
                Image.Height /= 1.2;
            }
        }

        #region wpf.ui

        private void ButtonSelectAll_Click(object sender, RoutedEventArgs args)
        {
            TextViewContent.SelectAll();
        }

        private void ButtonCopy_Click(object sender, RoutedEventArgs args)
        {
            TextViewContent.Copy();
        }

        private void ButtonCut_Click(object sender, RoutedEventArgs args)
        {
            TextViewContent.Cut();
        }

        private void ButtonPaste_Click(object sender, RoutedEventArgs args)
        {
            TextViewContent.Paste();
        }

        private void ButtonUndo_Click(object sender, RoutedEventArgs args)
        {
            TextViewContent.Undo();
        }

        private void ButtonRedo_Click(object sender, RoutedEventArgs args)
        {
            TextViewContent.Redo();
        }

        private void ButtonDelete_Click(object sender, RoutedEventArgs args)
        {
            TextViewContent.Delete();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private async void International_Click(object sender, RoutedEventArgs e)
        {
            try { if (!await OfficialLoaded()) return; }
            catch { //...
            }
        }

        private async void Steam_Click(object sender, RoutedEventArgs e)
        {
            try { if (!await SteamLoaded()) return; }
            catch { //...
            }
        }

        private async void Garena_Click(object sender, RoutedEventArgs e)
        {
            try { if (!await GarenaLoaded()) return; }
            catch { //...
            }
        }

        private async void Tencent_Click(object sender, RoutedEventArgs e)
        {
            try { if (!await TencentLoaded()) return; }
            catch { //...
            }
        }
        
        #endregion wpf.ui

        //private void TextViewContent_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.F)
        //    {
        //        FindReplaceDialog.Show(XCodeTextBox, FindReplaceDialog.SearchType.Find);
        //        e.Handled = true;
        //    }
        //}
    }
}