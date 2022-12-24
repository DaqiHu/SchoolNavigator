using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using SchoolNavigator.Models;
using Edge = SchoolNavigator.Models.Path;
using Path = System.Windows.Shapes.Path;

namespace SchoolNavigator
{
    /// <summary>
    /// MainWindow 页面
    /// </summary>
    public partial class MainWindow : Window
    {
        private Graph? _graph;
        private Ellipse[]? _verticEllipses;
        private Button[]? _locationButtons;
        private Path[]? _routePaths;

        // public ObservableCollection<WayPoint> WayPoints { get; set; } = new();

        public bool IsAdminMode { get; set; }
        private int _count;

        /// <summary>
        /// 构造方法
        /// </summary>
        public MainWindow()
        {
            // 初始化数据
            _graph = JsonConvert.DeserializeObject<Graph>(File.ReadAllText(@".\data\graph.json"));
            _graph.InitializePathWeights();

            // 初始化界面
            InitializeComponent();
            InitializeMainCanvas();
            InitializeItemsSources();

            // 初始化界面状态
            IsAdminMode = false;
            _count = 0;

            // TODO: 使用 ItemControl 实现途径节点显示，失败了，以后再试
            // DataContext = this;
            //
            // for (int i = 0; i < 10; i++)
            // WayPoints.Add(new WayPoint
            // {
            //     Name = "hello" + 1,
            //     Description = "world!"
            // });
        }

        /// <summary>
        /// 初始化UI列表，将后端数据绑定到ListBox，DataGrid等控件。
        /// </summary>
        private void InitializeItemsSources()
        {
            SortedSet<string> list = new SortedSet<string>();
            foreach (var location in _graph.Locations)
            {
                list.Add(location.Name);
            }

            StartPointComboBox.ItemsSource = list;
            EndPointComboBox.ItemsSource = list;
        }

        /// <summary>
        /// 初始化地图。将路径点、节点和路径元素添加到UI上。
        /// </summary>
        private void InitializeMainCanvas()
        {
            // 初始化路径，节点和景点。先加载的显示在下方的图层。
            SetPaths();
            SetVertices();
            SetLocations();
        }

        /// <summary>
        /// 初始化路径。
        /// </summary>
        private void SetPaths()
        {
            int size = _graph.Paths.Length;
            _routePaths = new Path[size];

            for (int i = 0; i < size; ++i)
            {
                _routePaths[i] = new Path
                {
                    // 绑定到 Route 资源，在 MainWindow.xaml -> Windows.Resources 中定义
                    Style = TryFindResource("Route") as Style,

                    // path 路径值
                    Data = Geometry.Parse(_graph.Paths[i].Data),
                    Name = _graph.Paths[i].Name
                };

                _routePaths[i].MouseLeftButtonDown += RoutePath_OnMouseLeftButtonDown;

                // 添加到 MainCanvas 并将其显示
                MainCanvas.Children.Add(_routePaths[i]);
            }
        }

        private void RoutePath_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsAdminMode) return;

            var name = ((Path)sender).Name;
            int id = -1;
            foreach (var path in _graph.Paths)
            {
                if (path.Name == name) id = path.Id;
            }

            if (_graph.Paths[id].IsEnabled)
            {
                _routePaths[id].Stroke = new SolidColorBrush(Colors.DarkRed);
                var path = _graph.Paths[id];
                path.IsEnabled = false;
                _graph.PathWeights[path.StartVerticeId][path.EndVerticeId] = 0;
                _graph.PathWeights[path.EndVerticeId][path.StartVerticeId] = 0;
                Debug.WriteLine($"path[{id}] is disabled.");
            }
            else
            {
                _routePaths[id].Stroke = new SolidColorBrush(Colors.MediumSeaGreen);
                var path = _graph.Paths[id];
                path.IsEnabled = true;
                _graph.PathWeights[path.StartVerticeId][path.EndVerticeId] = path.Distance;
                _graph.PathWeights[path.EndVerticeId][path.StartVerticeId] = path.Distance;
                Debug.WriteLine($"path[{id}] is enabled.");
            }

        }

        /// <summary>
        /// 初始化节点。
        /// </summary>
        private void SetVertices()
        {
            int size = _graph.Vertices.Length;
            _verticEllipses = new Ellipse[size];

            for (int i = 0; i < size; ++i)
            {
                _verticEllipses[i] = new Ellipse
                {
                    // 绑定到 WayPoint 资源，在 MainWindow.xaml -> Windows.Resources 中定义
                    Style = TryFindResource("WayPoint") as Style,
                };
                var ellipse = _verticEllipses[i];

                // 定义圆点的半径
                double r = 1.5;

                // 定义圆点的坐标
                ellipse.Height = 2 * r;
                ellipse.Width = 2 * r;
                Canvas.SetLeft(ellipse, _graph.Vertices[i].X - r);
                Canvas.SetTop(ellipse, _graph.Vertices[i].Y - r);

                MainCanvas.Children.Add(ellipse);
            }
        }

        /// <summary>
        /// 初始化目标点（景点）。
        /// </summary>
        private void SetLocations()
        {
            int size = _graph.Locations.Length;
            _locationButtons = new Button[size];

            for (int i = 0; i < _graph.Locations.Length; ++i)
            {
                _locationButtons[i] = new Button
                {
                    // 绑定到 Location 资源，在 MainWindow.xaml -> Windows.Resources 中定义
                    Style = TryFindResource("Location") as Style,
                    Name = _graph.Locations[i].Name,
                };
                var btn = _locationButtons[i];

                // 添加按钮事件
                btn.Click += Location_OnClick;
                btn.MouseRightButtonDown += Location_OnMouseRightButtonDown;

                // 设置控件的位置
                Canvas.SetLeft(btn, _graph.Locations[i].X);
                Canvas.SetTop(btn, _graph.Locations[i].Y);

                MainCanvas.Children.Add(btn);
            }
        }

        /// <summary>
        /// 点击“计算路径”按钮触发事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalculateRoute_OnClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Calculate Route");

            // TODO: 改用 ComboBox 的 invalid hint 功能
            if (_count == 2)
            {
                if (IsLocationValid())
                {
                    ShowRoute();
                    InfoTab.SelectedIndex = 1;
                }
                else
                {
                    MessageBox.Show("不合法的起点或终点。");
                }
            }
            else
            {
                MessageBox.Show("缺少起点或终点。");
            }
        }

        /// <summary>
        /// 检查起点、终点的合法性
        /// </summary>
        /// <returns>一个布尔值。代表起点终点是否存在。</returns>
        private bool IsLocationValid()
        {
            List<string> list = new List<string>();
            foreach (var location in _graph.Locations)
            {
                if (location.Name == StartPointComboBox.Text ||
                    location.Name == EndPointComboBox.Text)
                    list.Add(location.Name);
            }

            if (list.Count == 2 && list[0] != list[1]) return true;
            else return false;
        }

        /// <summary>
        /// 点击“清除信息”按钮触发事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EraseInfo_OnClick(object sender, RoutedEventArgs e)
        {
            EraseAllEventHandler(clear: true);

            // ClearCanvas();
            // StartPointComboBox.Text = "";
            // EndPointComboBox.Text = "";
            // EraseInfoButton.IsEnabled = false;
        }

        /// <summary>
        /// 点击“禁用路线”按钮触发事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisableRoute_OnClick(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// 点击地图上的路径点触发事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Location_OnClick(object sender, RoutedEventArgs e)
        {
            var name = ((Button)sender).Name;
            if (_count == 0) ComboBoxHandler(1, name);
            else if (_count == 1)
                if (string.IsNullOrEmpty(EndPointComboBox.Text))
                    ComboBoxHandler(2, name);
                else
                    ComboBoxHandler(1, name);

            EraseAllEventHandler();
        }

        /// <summary>
        /// 鼠标右键地图上的路径点触发事件。在Debug阶段用于快速验证算法，Release中不可用。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Location_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            // if (_count == 1)
            // {
            //     end = ((Button)sender).Name;
            //     ShowRoute();
            //     _count = 0;
            // }
            // else
            // {
            //     start = ((Button)sender).Name;
            // }
            //
            // _count++;
#else
                return;
#endif
        }

        /// <summary>
        /// 显示地点介绍
        /// </summary>
        /// <param name="name"></param>
        public void ShowLocationDetails(string name)
        {
            foreach (var location in _graph.Locations)
            {
                if (location.Name == name)
                {
                    IntroduceBoxEmptyText.Visibility = Visibility.Hidden;
                    if (IntroduceBox1.Header == "")
                    {
                        IntroduceBox1.Header = name;
                        IntroduceBoxText1.Text = location.Info;
                        IntroduceBox1.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        IntroduceBox2.Header = name;
                        IntroduceBoxText2.Text = location.Info;
                        IntroduceBox2.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        /// <summary>
        /// 清除画板中的路线和节点显示。
        /// </summary>
        private void ClearCanvas()
        {
            // _count = 0;
            // start = string.Empty;
            // end = string.Empty;

            for (int i = 0; i < _graph.Paths.Length; ++i)
            {
                _routePaths[i].Visibility = Visibility.Hidden;
            }

            for (int i = 0; i < _graph.Vertices.Length; ++i)
            {
                _verticEllipses[i].Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// 显示路径。通过Dijkstra算法计算最短路径并展示在界面上。
        /// </summary>
        private void ShowRoute()
        {
            string start = StartPointComboBox.Text;
            string end = EndPointComboBox.Text;
            Debug.WriteLine("Show Route");


            // int startIndex = Array.IndexOf(_locationButtons, start);
            // int endIndex = Array.IndexOf(_locationButtons, end);

            // TODO
            Debug.WriteLine($"from {start} to {end}.");

            int startIndex = -1;
            int endIndex = -1;

            // TODO: 可否用 lambda 表达式求index？
            // int lambdaStartIndex = Array.FindIndex(_graph.Locations, location => location.Name == start);
            // int lambdaEndIndex = Array.FindIndex(_graph.Locations, location => location.Name == end);


            foreach (var location in _graph.Locations)
            {
                if (location.Name == end) endIndex = location.Id;
                if (location.Name == start) startIndex = location.Id;
                if (endIndex != -1 && startIndex != -1) break;
            }

            // 使用 Dijkstra 算法，得到 startIndex 到各点的距离和前驱节点信息。
            var (distances, prePoints) = _graph.Dijkstra(startIndex);

            if (distances[endIndex] == double.MaxValue)
            {
                MessageBox.Show("找不到最短路径！没有可用的路线。");
                RouteDistanceText.Text = "???m";
                return;
            }

            // 存储所有路径点的列表
            List<int> all = new List<int> { endIndex };

            // 找到所有路径点
            int tempIndex = endIndex;
            while (prePoints[tempIndex] != startIndex)
            {
                tempIndex = prePoints[tempIndex];
                all.Add(tempIndex);
            }

            all.Add(startIndex);

            // 显示所有路径和节点
            for (int i = 0; i < all.Count - 1; ++i)
            {
                foreach (var path in _graph.Paths)
                {
                    if (path.StartVerticeId == all[i] && path.EndVerticeId == all[i + 1] ||
                        path.EndVerticeId == all[i] && path.StartVerticeId == all[i + 1])
                    {
                        _routePaths[path.Id].Visibility = Visibility.Visible;

                        if (path.StartVerticeId < _verticEllipses.Length)
                            _verticEllipses[path.StartVerticeId].Visibility = Visibility.Visible;
                        if (path.EndVerticeId < _verticEllipses.Length)
                            _verticEllipses[path.EndVerticeId].Visibility = Visibility.Visible;
                        break;
                    }
                }
            }

            // 60 pixel = 50m
            var result = $"{distances[endIndex] * 5 / 6:F1}m";
            RouteDistanceText.Text = result;
            Debug.WriteLine(result);
        }

        private void RouteDistanceTextHandler(double distance)
        {
        }

        /// <summary>
        /// 点击关闭窗口按钮时触发。将保存禁用路线的信息。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            var jsonString = JsonConvert.SerializeObject(_graph, Formatting.Indented);
            File.WriteAllText(@".\data\graph.json", jsonString);
        }

        /// <summary>
        /// 鼠标右键紫金学院Logo进入管理员模式，触发事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnterAdminMode_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            AdminModeHandler(enable: !IsAdminMode);
        }

        private void AdminModeHandler(bool enable = false)
        {
            IsAdminMode = enable;
            Title.Text = enable ? "管理员模式" : "紫金学院校园导航系统";
            TitleBar.Mode = enable ? ColorZoneMode.SecondaryDark : ColorZoneMode.PrimaryDark;

            var disabledPaths = new HashSet<int>();
            foreach (var path in _graph.Paths)
            {
                if (path.IsEnabled == false)
                    disabledPaths.Add(path.Id);
            }

            if (enable)
            {
                for (var i = 0; i < _routePaths.Length; i++)
                {
                    _routePaths[i].Visibility = Visibility.Visible;
                    if (disabledPaths.Contains(i))
                        _routePaths[i].Stroke = new SolidColorBrush(Colors.DarkRed);
                    else
                        _routePaths[i].Stroke = new SolidColorBrush(Colors.LightSeaGreen);
                }

                foreach (var location in _locationButtons)
                {
                    location.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                foreach (var path in _routePaths)
                {
                    path.Stroke = new SolidColorBrush(Colors.OrangeRed);
                    path.Visibility = Visibility.Hidden;
                }

                foreach (var location in _locationButtons)
                {
                    location.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// 起点路线：选择下拉框中文本改变时触发事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        // private void StartPointComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        // {
        //     Debug.WriteLine($"The text is {StartPointComboBox.Text}");
        //
        //     ComboBoxHandler(1);
        //     EraseAllEventHandler();
        // }

        /// <summary>
        /// 终点路线：选择下拉框中文本改变时触发事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        // private void EndPointComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        // {
        //     Debug.WriteLine($"The text is {StartPointComboBox.Text}");
        //
        //     ComboBoxHandler(2, EndPointComboBox.Text);
        //     EraseAllEventHandler();
        // }

        /// <summary>
        /// 鼠标右键标题栏清除地图信息，仅在Debug时可用。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Title_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            EraseAllEventHandler(true);
#else
            return;
#endif
        }


        /// <summary>
        /// 清除所有信息。
        /// </summary>
        /// <param name="clearCanvas">是否清除地图界面。</param>
        private void EraseAllEventHandler(bool clear = false, bool clearCanvas = true)
        {
            if (clear)
            {
                // 清除起点、终点选择
                ComboBoxHandler(1, clear: true);
                ComboBoxHandler(2, clear: true);

                // 清除景点介绍并初始化
                IntroduceBoxHandler(1, enable: false);
                IntroduceBoxHandler(2, enable: false);


                // 清除具体路径信息


                // 清除地图路线
                if (clearCanvas)
                    ClearCanvas();


                // 禁用清除按钮
                EraseInfoButton.IsEnabled = false;
                InfoTab.SelectedIndex = 0;
            }
            else
            {
                EraseInfoButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 对选择起点、终点的ComboBox进行操作。
        /// </summary>
        /// <param name="number">1为起点，2为终点。</param>
        /// <param name="text">设置ComboBox显示的Text</param>
        /// <exception cref="ArgumentException">number为1或者2以外的数时抛异常。</exception>
        private void ComboBoxHandler(int number, string text = "", bool clear = false)
        {
            switch (number)
            {
                case 1:
                    if (StartPointComboBox.Text == "") StartPointComboBox.Text = text;
                    if (clear) StartPointComboBox.Text = string.Empty;
                    IntroduceBoxHandler(1, StartPointComboBox.Text);
                    break;
                case 2:
                    if (EndPointComboBox.Text == "") EndPointComboBox.Text = text;
                    if (clear) EndPointComboBox.Text = string.Empty;
                    IntroduceBoxHandler(2, EndPointComboBox.Text);
                    break;
                default:
                    throw new ArgumentException("不合法的参数");
            }

            int count = 0;
            if (StartPointComboBox.Text != "") count++;
            if (EndPointComboBox.Text != "") count++;
            _count = count;
        }

        /// <summary>
        /// 对显示地点信息的 IntroduceBox 控件进行操作。
        /// </summary>
        /// <param name="number">1为起点信息，2为终点信息。</param>
        /// <param name="header">设置信息的标题。通过标题可以自动匹配到内容。</param>
        /// <param name="enable">是否启用。可以通过设置为 false 将其禁用。</param>
        /// <returns>返回IntroduceBox有Header内容的个数。可能值为 0, 1, 2。</returns>
        /// <exception cref="ArgumentException"></exception>
        private int IntroduceBoxHandler(int number, string header = "", bool enable = true)
        {
            // 查找 header 对应的描述信息
            string text = "";
            if (header != "" && header != null)
                foreach (var location in _graph.Locations)
                {
                    if (header == location.Name)
                    {
                        text = location.Info;
                        break;
                    }
                }
            else enable = false;

            switch (number)
            {
                case 1:
                    IntroduceBox1.Header = header;
                    IntroduceBoxText1.Text = text;
                    IntroduceBox1.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case 2:
                    IntroduceBox2.Header = header;
                    IntroduceBoxText2.Text = text;
                    IntroduceBox2.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
                    break;
                default: throw new ArgumentException("不合法的传入值");
            }

            if (IntroduceBox1.Visibility == Visibility.Collapsed &&
                IntroduceBox2.Visibility == Visibility.Collapsed)
                IntroduceBoxEmptyText.Visibility = Visibility.Visible;
            else
                IntroduceBoxEmptyText.Visibility = Visibility.Collapsed;

            int count = 0;
            if (IntroduceBox1.Header != "") count++;
            if (IntroduceBox2.Header != "") count++;
            return count;
        }

        /// <summary>
        /// “起点”内容改变时触发事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartPointComboBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Debug.WriteLine($"The text is {StartPointComboBox.Text}");

            ComboBoxHandler(1);
            EraseAllEventHandler();
        }

        /// <summary>
        /// “终点”内容改变时触发事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EndPointComboBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            Debug.WriteLine($"The text is {StartPointComboBox.Text}");

            ComboBoxHandler(2);
            EraseAllEventHandler();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("overrides material design button check event.");
        }
    }

    // public class WayPoint
    // {
    //     public string? Name { get; set; }
    //     public string? Description { get; set; }
    // }
}