/********************************************************************************
 *   This file is part of NRtfTree Library.
 *
 *   NRtfTree Library is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU Lesser General Public License as published by
 *   the Free Software Foundation; either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   NRtfTree Library is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU Lesser General Public License for more details.
 *
 *   You should have received a copy of the GNU Lesser General Public License
 *   along with this program. If not, see <http://www.gnu.org/licenses/>.
 ********************************************************************************/

/********************************************************************************
 * Library:		NRtfTree
 * Version:     v0.3.0
 * Date:		02/09/2007
 * Copyright:   2007 Salvador Gomez
 * E-mail:      sgoliver.net@gmail.com
 * Home Page:	http://www.sgoliver.net
 * SF Project:	http://nrtftree.sourceforge.net
 *				http://sourceforge.net/projects/nrtftree
 * Class:		RtfTextFormat
 * Description:	Representa un formato de texto.
 * ******************************************************************************/

using System.Drawing;

namespace NRtfTree
{
    namespace Util
    {
        /// <summary>
        /// Representa un formato de texto.
        /// </summary>
        public class RtfTextFormat
        {
            /// <summary>
            /// Negrita.
            /// </summary>
            public bool bold = false;

            /// <summary>
            /// Cursiva.
            /// </summary>
            public bool italic = false;

            /// <summary>
            /// Subrayado.
            /// </summary>
            public bool underline = false;

            /// <summary>
            /// Nombre de la fuente.
            /// </summary>
            public string font = "Arial";

            /// <summary>
            /// Tamaño de la fuente.
            /// </summary>
            public int size = 10;

            /// <summary>
            /// Color de la fuente.
            /// </summary>
            public Color color = Color.Black;
        }
    }
}
