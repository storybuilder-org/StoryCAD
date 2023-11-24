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
 * Class:		RtfColorTable
 * Description:	Tabla de Colores de un documento RTF.
 * ******************************************************************************/

using System.Collections.Generic;
using System.Drawing;

namespace NRtfTree
{
    namespace Util
    {
        /// <summary>
        /// Tabla de colores de un documento RTF.
        /// </summary>
        public class RtfColorTable
        {
            /// <summary>
            /// Lista interna de colores.
            /// </summary>
            List<int> colors;

            /// <summary>
            /// Constructor de la clase RtfColorTable.
            /// </summary>
            public RtfColorTable()
            {
                colors = new List<int>();
            }

            /// <summary>
            /// Inserta un nuevo color en la tabla de colores.
            /// </summary>
            /// <param name="color">Nuevo color a insertar.</param>
            public void AddColor(Color color)
            {
                colors.Add(color.ToArgb());
            }

            /// <summary>
            /// Obtiene el color n-ésimo de la tabla de colores.
            /// </summary>
            /// <param name="index">Indice del color a recuperar.</param>
            /// <returns>Color n-ésimo de la tabla de colores.</returns>
            public Color this[int index] => Color.FromArgb(colors[index]);

            /// <summary>
            /// Número de colores en la tabla.
            /// </summary>
            public int Count => colors.Count;

            /// <summary>
            /// Obtiene el índice de un color determinado en la tabla.
            /// </summary>
            /// <param name="color">Color a consultar.</param>
            /// <returns>Indice del color consultado.</returns>
            public int IndexOf(Color color)
            {
                return colors.IndexOf(color.ToArgb());
            }
        }
    }
}
