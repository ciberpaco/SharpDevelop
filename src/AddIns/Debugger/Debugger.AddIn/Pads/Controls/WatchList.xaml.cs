﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Debugger.AddIn.TreeModel;
using ICSharpCode.Core.Presentation;
using ICSharpCode.SharpDevelop.Debugging;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Gui.Pads;

namespace Debugger.AddIn.Pads.Controls
{
	public enum WatchListType
	{
		LocalVar,
		Watch
	}
	
	public partial class WatchList : UserControl
	{
		private ObservableCollection<TreeNode> items = new ObservableCollection<TreeNode>();
		
		public WatchList()
		{
			InitializeComponent();
		}
		
		public WatchListType WatchType { get; set; }
		
		public DebuggerPad ParentPad { get; set; }
		
		public ObservableCollection<TreeNode> WatchItems { get { return items; } }
		
		public TreeNode SelectedNode {
			get {
				return this.MyList.SelectedItem as TreeNode;
			}
		}
		
		void OnValueTextBoxKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter && e.Key != Key.Escape)
			{
				e.Handled = true;
				return;
			}
			
			if (e.Key == Key.Enter) {
				if(SelectedNode is ExpressionNode) {
					var node = (ExpressionNode)SelectedNode;
					node.SetText(((TextBox)sender).Text);
				}
			}
			if (e.Key == Key.Enter || e.Key == Key.Escape) {
				for (int i = 0; i < MyList.Items.Count; i++) {
					TreeViewItem child = (TreeViewItem)MyList.ItemContainerGenerator.ContainerFromIndex(i);
					child.IsSelected = false;
				}
			}
		}
		
		void WatchListAutoCompleteCell_CommandEntered(object sender, EventArgs e)
		{
			if (SelectedNode == null) return;
			if (WatchType != WatchListType.Watch) return;
			
			var cell = ((WatchListAutoCompleteCell)sender);
			
			SelectedNode.Name = cell.CommandText;
			if (WatchType == WatchListType.Watch) {
				ParentPad.RefreshPad();
			}
			
			for (int i = 0; i < MyList.Items.Count; i++) {
				TreeViewItem child = (TreeViewItem)MyList.ItemContainerGenerator.ContainerFromIndex(i);
				child.IsSelected = false;
			}
			
			// find TreeviewItem
			var treeViewItem = WpfTreeNavigation.TryFindParent<TreeViewItem>(cell);
			if (treeViewItem == null) return;
			
			// find textblock
			var tb = WpfTreeNavigation.TryFindChild<TextBlock>(treeViewItem);
			if (tb == null) return;
			
			// change visibility
			cell.Visibility = Visibility.Collapsed;
			tb.Visibility = Visibility.Visible;
		}
		
		void MyList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (WatchType != WatchListType.Watch)
				return;
			
			if (e.OriginalSource is TextBlock) {
				var tb = (TextBlock)e.OriginalSource;
				
				// find TreeviewItem
				var treeViewItem = WpfTreeNavigation.TryFindParent<TreeViewItem>(tb);
				if (treeViewItem == null) return;
				
				// try find TreeViewItem parent of the current node 
				// if parent != null, we will not alow edit the value
				var treeViewItemParent = WpfTreeNavigation.TryFindParent<TreeViewItem>(treeViewItem, false);
				if (treeViewItemParent != null) return;
				
				// find cell
				var cell = WpfTreeNavigation.TryFindChild<WatchListAutoCompleteCell>(treeViewItem);
				
				// change visibility
				tb.Visibility = Visibility.Collapsed;
				cell.Visibility = Visibility.Visible;
			}
		}
	}
}