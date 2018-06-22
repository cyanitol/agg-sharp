﻿/*
Copyright (c) 2014, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System;
using System.Collections.Generic;

namespace MatterHackers.Agg.UI
{
	public interface IHardBreak
	{
	}

	public interface ISkipIfFirst
	{
	}

	public class FlowLeftRightWithWrapping : FlowLayoutWidget
	{
		protected List<GuiWidget> addedChildren = new List<GuiWidget>();

		public HAnchor RowFlowAnchor { get; set; } = HAnchor.Left | HAnchor.Fit;
		public BorderDouble RowMargin { get; set; } = new BorderDouble(3, 0);
		public BorderDouble RowPadding { get; set; } = new BorderDouble(3);

		public FlowLeftRightWithWrapping()
			: base(FlowDirection.TopToBottom)
		{
			HAnchor = HAnchor.Stretch;
		}

		public override void OnParentChanged(EventArgs e)
		{
			if (Parent != null)
			{
				Parent.BoundsChanged += Parent_BoundsChanged;
			}
			base.OnParentChanged(e);
		}

		bool doingLayout = false;
		double oldWidth = 0;
		private void Parent_BoundsChanged(object sender, EventArgs e)
		{
			var parent = Parent;
			if (parent != null
				&& parent.Width != oldWidth)
			{
				if (!doingLayout)
				{
					DoWrappingLayout();
				}
				oldWidth = parent.Width;
			}
		}

		public override void AddChild(GuiWidget childToAdd, int indexInChildrenList = -1)
		{
			addedChildren.Add(childToAdd);
		}

		protected void DoWrappingLayout()
		{
			using (this.LayoutLock())
			{
				doingLayout = true;
				// remove all the children we added
				foreach (var child in addedChildren)
				{
					if (child.Parent != null)
					{
						using (child.Parent.LayoutLock())
						{
							child.Parent.RemoveChild(child);
							child.ClearRemovedFlag();
						}
					}
				}

				// close all the row containers
				this.CloseAllChildren();

				// add in new row container
				FlowLayoutWidget childContainerRow = new FlowLayoutWidget()
				{
					Margin = RowMargin,
					Padding = RowPadding,
					HAnchor = RowFlowAnchor,
				};
				base.AddChild(childContainerRow);

				double runningSize = 0;
				foreach (var child in addedChildren)
				{
					if (Parent != null
						&& (runningSize + child.Width > Parent.Width
							|| child is IHardBreak))
					{
						runningSize = 0;
						if (childContainerRow != null)
						{
							childContainerRow.PerformLayout();
						}
						childContainerRow = new FlowLayoutWidget()
						{
							Margin = RowMargin,
							Padding = RowPadding,
							HAnchor = RowFlowAnchor,
						};

						base.AddChild(childContainerRow);
					}

					if (runningSize > 0 
						|| !(child is ISkipIfFirst))
					{
						// add the new child to the current row
						using (childContainerRow.LayoutLock())
						{
							childContainerRow.AddChild(child);
						}
						runningSize += child.Width;
					}
				}
				if (childContainerRow != null)
				{
					childContainerRow.PerformLayout();
				}
				doingLayout = false;
			}
			this.PerformLayout();
		}
	}
}