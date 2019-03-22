// Copyright 2019 The Poderosa Project.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Poderosa.View;
using System;
using System.Drawing;

namespace Poderosa.Document {

    /// <summary>
    /// An interface of the document object that is displayed by <see cref="CharacterDocumentViewer"/>.
    /// </summary>
    public interface ICharacterDocument {

        object SyncRoot {
            get;
        }

        InvalidatedRegion InvalidatedRegion {
            get;
        }

        /// <summary>
        /// Gets range of the row ID in this document.
        /// </summary>
        /// <returns>span of the row ID</returns>
        RowIDSpan GetRowIDSpan();

        /// <summary>
        /// Determines which color should be used as the background color of this document.
        /// </summary>
        /// <param name="profile">current profile</param>
        /// <returns>background color</returns>
        Color DetermineBackgroundColor(RenderProfile profile);

        /// <summary>
        /// Determines which image should be painted (or should not be painted) in the background of this document.
        /// </summary>
        /// <param name="profile">current profile</param>
        /// <returns>an image object to paint, or null.</returns>
        Image DetermineBackgroundImage(RenderProfile profile);

        /// <summary>
        /// Apply action to each row in the specified range.
        /// </summary>
        /// <remarks>
        /// This method must guarantee that the specified action is called for all rows in the specified range.
        /// If a row was missing in this document, null is passed to the action.
        /// </remarks>
        /// <param name="startRowID">start Row ID</param>
        /// <param name="rows">number of rows</param>
        /// <param name="action">
        /// a delegate function to apply. the first argument is a row ID. the second argument is a target GLine object.
        /// </param>
        void ForEach(int startRowID, int rows, Action<int, GLine> action);

        /// <summary>
        /// Apply action to the specified row.
        /// </summary>
        /// <remarks>
        /// If a row was missing in this document, null is passed to the action.
        /// </remarks>
        /// <param name="rowID">Row ID</param>
        /// <param name="action">
        /// a delegate function to apply. the first argument may be null.
        /// </param>
        void Apply(int rowID, Action<GLine> action);

        /// <summary>
        /// Notifies document implementation from the document viewer
        /// that the size of the visible area was changed.
        /// </summary>
        /// <param name="rows">number of visible rows</param>
        /// <param name="cols">number of visible columns</param>
        void VisibleAreaSizeChanged(int rows, int cols);
    }
}
