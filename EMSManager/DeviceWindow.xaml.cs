using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TemplateProject
{
    /// <summary>
    /// Interaction logic for NavWindow.xaml
    /// </summary>
    public partial class DeviceWindow : Window
    {
        NavWindow navWindow;
        DataTemplate headerTemplate1;
        DataTemplate headerTemplate2;

        public DeviceWindow(NavWindow navWindow)
        {
            InitializeComponent();

            this.navWindow = navWindow;
            this.Left = -this.Width;
            this.Topmost = true;

            headerTemplate1 = (DataTemplate)treeView1.FindResource("headerTemplate1");
            headerTemplate2 = (DataTemplate)treeView1.FindResource("headerTemplate2");
            InitTreeView();
        }

        private void InitTreeView()
        {
            treeView1.Items.Clear();
            foreach (KeyValuePair<string, Location> pair in CachedMap.Instance.dicLocation)
            {
                string locationkey = pair.Key;
                string locationName = pair.Value.display_name;
                TreeViewItem item = CreateItemForLocation(locationName, locationkey);
                treeView1.Items.Add(item);
            }
            treeView1.MouseDoubleClick += treeView1_MouseDoubleClick;
        }

        void treeView1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Nav_Deactivated(null, null);
        }

        private TreeViewItem CreateItemForLocation(string locationName, string locationkey)
        {
            TreeViewItem itemStation = new TreeViewItem();
            itemStation.Header = locationName;
            itemStation.IsExpanded = true;
            itemStation.HeaderTemplate = headerTemplate1;

            AddChildForItem(itemStation, locationkey);
            return itemStation;
        }

        private void AddChildForItem(TreeViewItem itemParent, string locationkey, string parentkey = "")
        {
            List<EmsNode> listChild;
            if (parentkey == "")        //top level in this location
                listChild = CachedMap.Instance.dicNode.Values.Where(p => p.parentkey == "" && p.locationkey == locationkey).ToList();
            else
                listChild = CachedMap.Instance.dicNode.Values.Where(p => p.parentkey == parentkey && p.locationkey == locationkey).ToList();

            if (listChild.Count == 0)
                return;

            foreach (EmsNode node in listChild)
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = node.name;
                item.IsExpanded = true;
                //item.PreviewMouseDoubleClick += item_MouseDoubleClick;
                item.GiveFeedback += item_GiveFeedback;
                item.Tag = node.pkey;
                itemParent.Items.Add(item);

                if (hasChild(node.pkey))
                {
                    item.HeaderTemplate = headerTemplate1;
                    AddChildForItem(item, locationkey, node.pkey);
                }
                else
                {
                    item.HeaderTemplate = headerTemplate2;
                }
            }
        }

        private bool hasChild(string parentkey)
        {
            return CachedMap.Instance.dicNode.Values.ToList().Exists(p => p.parentkey == parentkey);
        }

        public void AnimationShow()
        {
            DoubleAnimation animation = new DoubleAnimation(-this.Width, 0, TimeSpan.FromSeconds(0.3));
            //animation.RepeatBehavior = 1;
            //animation.AutoReverse = true;
            Storyboard.SetTargetName(animation, "Nav");
            Storyboard.SetTargetProperty(animation, new PropertyPath(Window.LeftProperty));
            // Create a storyboard to contain the animation.
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            this.Show();
            this.Activate();
            storyboard.Begin(this);
        }

        public void AnimationHide()
        {
            //this.Show();
            DoubleAnimation animation = new DoubleAnimation(0, -this.Width, TimeSpan.FromSeconds(0.3));
            Storyboard.SetTargetName(animation, "Nav");
            Storyboard.SetTargetProperty(animation, new PropertyPath(Window.LeftProperty));
            // Create a storyboard to contain the animation.
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            this.Show();
            this.Activate();
            storyboard.Begin(this);
        }

        private void Nav_Deactivated(object sender, EventArgs e)
        {
            this.AnimationHide();
            this.Hide();
        }

        private bool IsLeaf(TreeViewItem treeViewItem)
        {
            if (treeViewItem.HasItems)
                return false;
            else
                return true;
        }

        private string GetItemType(TreeViewItem treeViewItem)
        {
            if (treeViewItem == null)
                return null;

            if (treeViewItem.Header.ToString().ToLower().Contains("transformer"))
            {
                return "Transformer";
            }
            else if (treeViewItem.Header.ToString().ToLower().Contains("switchgear"))
            {
                return "Switchgear";
            }
            else if (treeViewItem.Header.ToString().ToLower().Contains("converter"))
            {
                return "Converter";
            }
            else if (treeViewItem.Header.ToString().ToLower().Contains("batteries"))
            {
                return "Batteries";
            }

            TreeViewItem itemParent = treeViewItem.Parent as TreeViewItem;
            if (itemParent == null)
                return null;
            else
                return GetItemType(itemParent);
        }

        private void item_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //string currentWin = this.GetType().Name;
            TreeViewItem item = (TreeViewItem)e.Source;
            if (!item.IsSelected)       //double click will trigger event for all the parent node
                return;
            if (IsLeaf(item) == false)
                return;
            string itemType = GetItemType(item);
            //do special check for DGA
            string targetWindow = "";
            if (itemType.ToLower().Contains("transformer"))
            {
                targetWindow = "Power_DGA_DTM1";
            }
            else if (itemType.ToLower().Contains("switchgear"))
            {   //It is a DataNode
                targetWindow = "PowerDashboard";
            }
            else if (itemType.ToLower().Contains("converter"))
            {   //It is a DataPoint
                targetWindow = "PowerAnalysis";
            }
            //navWindow.ActivateAndSetDevice(targetWindow, item.Header.ToString());
        }

        private void item_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            {
                Cursor cursor = CursorHelper.CreateCursor(e.Source as UIElement);
                e.UseDefaultCursors = false;
                Mouse.SetCursor(cursor);
            }

            e.Handled = true;
        }

        private void treeView1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //TreeViewItem item = (TreeViewItem)e.Source;
            TreeViewItem item = (TreeViewItem)e.NewValue;
            if (!item.IsSelected)       //double click will trigger event for all the parent node
                return;
            if (IsLeaf(item) == false)
                return;
            string targetWindow = "能耗数据查询";
            navWindow.ActivateAndSetDevice(targetWindow, item.Tag);
        }

    }
}
