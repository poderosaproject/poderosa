/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: MultiSelectTreeView.cs,v 1.2 2011/11/30 23:21:29 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

namespace Poderosa.SFTP {
    
    /// <summary>
    /// TreeView with multi-select functionality
    /// </summary>
    /// <remarks>
    /// <para>SelectedNode property doesn't work.</para>
    /// </remarks>
    internal class MultiSelectTreeView : TreeView {

        private readonly List<TreeNode> _selectedNodes = new List<TreeNode>();

        public event TreeViewEventHandler SingleNodeSelected;

        public event EventHandler SelectedNodesChanged;


        public new TreeNode SelectedNode {
            get {
                throw new InvalidOperationException();
            }
            set {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Gets array of the selected nodes.
        /// </summary>
        public TreeNode[] SelectedNodes {
            get {
                ReduceSelectedNodes();
                return _selectedNodes.ToArray();
            }
        }

        /// <summary>
        /// Gets number of the selected nodes.
        /// </summary>
        public int SelectedNodeCount {
            get {
                ReduceSelectedNodes();
                return _selectedNodes.Count;
            }
        }

        /// <summary>
        /// Select single node
        /// </summary>
        public void SelectNode(TreeNode node) {
            ClearSelectedNodes();
            AddToSelectedNodes(node);
            if (SingleNodeSelected != null)
                SingleNodeSelected(this, new TreeViewEventArgs(node, TreeViewAction.Unknown));
            if (SelectedNodesChanged != null)
                SelectedNodesChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Unselect all nodes
        /// </summary>
        public void UnselectAllNodes() {
            ClearSelectedNodes();
            if (SelectedNodesChanged != null)
                SelectedNodesChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Overrides OnBeforeSelect()
        /// </summary>
        protected override void OnBeforeSelect(TreeViewCancelEventArgs e) {
            e.Cancel = true;    // default behavior is not used

            if (e.Action != TreeViewAction.ByMouse && e.Action != TreeViewAction.ByKeyboard)
                return;

            Keys key = Control.ModifierKeys;
            TreeNode selNode = e.Node;

#if DEBUG
            Debug.WriteLine(String.Format("OnBeforeSelect: \"{0}\" {1}", selNode.Text, key));
#endif
            if (key == Keys.Control || key == Keys.Shift) {
                if (!IsSameLevelWithSelectedNodes(selNode)) {
                    ClearSelectedNodes();
                }
            }

            if (key == Keys.Control && _selectedNodes.Count > 0) {
                if (_selectedNodes.Contains(selNode)) {
                    RemoveFromSelectedNodes(selNode);
                    if (SelectedNodesChanged != null)
                        SelectedNodesChanged(this, EventArgs.Empty);
                }
                else {
                    AddToSelectedNodes(selNode);
                    if (SelectedNodesChanged != null)
                        SelectedNodesChanged(this, EventArgs.Empty);
                }
            }
            else if (key == Keys.Shift && _selectedNodes.Count > 0) {
                TreeNode firstNode = _selectedNodes[0];
                Debug.Assert(Object.ReferenceEquals(firstNode.Parent, selNode.Parent));

                TreeNodeCollection collection;
                if (selNode.Parent == null)
                    collection = this.Nodes;
                else
                    collection = selNode.Parent.Nodes;

                int firstIndex = firstNode.Index;
                int lastIndex = selNode.Index;

                ClearSelectedNodes();

                if (firstIndex <= lastIndex) {
                    for (int i = firstIndex; i <= lastIndex; i++) {
                        TreeNode node = collection[i];
                        AddToSelectedNodes(node);
                    }
                }
                else {
                    for (int i = firstIndex; i >= lastIndex; i--) {
                        TreeNode node = collection[i];
                        AddToSelectedNodes(node);
                    }
                }

                if (SelectedNodesChanged != null)
                    SelectedNodesChanged(this, EventArgs.Empty);
            }
            else {
                ClearSelectedNodes();
                AddToSelectedNodes(selNode);

                if (key != Keys.Control && key != Keys.Shift) {
                    if (SingleNodeSelected != null)
                        SingleNodeSelected(this, new TreeViewEventArgs(selNode, e.Action));
                }
                if (SelectedNodesChanged != null)
                    SelectedNodesChanged(this, EventArgs.Empty);
            }

#if DEBUG
            StringBuilder s = new StringBuilder();
            s.Append("Selected: ");
            foreach (TreeNode node in _selectedNodes) {
                s.Append('"').Append(node.Text).Append('"').Append(", ");
            }
            Debug.WriteLine(s.ToString());
            TreeNode singleSel = base.SelectedNode;
            Debug.WriteLine("SelectedNode: " + (singleSel == null ? "(null)" : singleSel.Text));
#endif
        }

        private void ClearSelectedNodes() {
            ReduceSelectedNodes();
            foreach (TreeNode node in _selectedNodes) {
                SetUnselectedVisual(node);
            }
            _selectedNodes.Clear();
        }

        private void ReduceSelectedNodes() {
            for (int i = 0; i < _selectedNodes.Count; ) {
                if (_selectedNodes[i].TreeView == null)
                    _selectedNodes.RemoveAt(i);
                else
                    i++;
            }
        }

        private bool IsSameLevelWithSelectedNodes(TreeNode node) {
            TreeNode parent = node.Parent;
            foreach (TreeNode tn in _selectedNodes) {
                if (!Object.ReferenceEquals(tn.Parent, parent)) {
                    return false;
                }
            }
            return true;
        }

        private void AddToSelectedNodes(TreeNode node) {
            _selectedNodes.Add(node);
            SetSelectedVisual(node);
        }

        private void RemoveFromSelectedNodes(TreeNode node) {
            _selectedNodes.Remove(node);
            SetUnselectedVisual(node);
        }

        private void SetSelectedVisual(TreeNode node) {
            node.BackColor = SystemColors.Highlight;
            node.ForeColor = SystemColors.HighlightText;
        }

        private void SetUnselectedVisual(TreeNode node) {
            node.BackColor = this.BackColor;
            node.ForeColor = this.ForeColor;
        }
    }
}
