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
 * Class:		RtfFontTable
 * Description:	Tabla de Fuentes de un documento RTF.
 * ******************************************************************************/

using System.Collections.Generic;

namespace NRtfTree
{
    namespace Util
    {
        /// <summary>
        /// Tabla de fuentes de un documento RTF.
        /// </summary>
        public class RtfFontTable
        {
            /// <summary>
            /// Lista interna de fuentes.
            /// </summary>
            List<string> fonts;

            /// <summary>
            /// Constructor de la clase RtfFontTable.
            /// </summary>
            public RtfFontTable()
            {
                fonts = new List<string>();
            }

            /// <summary>
            /// Inserta un nueva fuente en la tabla de fuentes.
            /// </summary>
            /// <param name="name">Nueva fuente a insertar.</param>
            public void AddFont(string name)
            {
                fonts.Add(name);
            }

            /// <summary>
            /// Obtiene la fuente n-ésima de la tabla de fuentes.
            /// </summary>
            /// <param name="index">Indice de la fuente a recuperar.</param>
            /// <returns>Fuente n-ésima de la tabla de fuentes.</returns>
            public string this[int index] => fonts[index];

            /// <summary>
            /// Número de fuentes en la tabla.
            /// </summary>
            public int Count => fonts.Count;

            /// <summary>
            /// Obtiene el índice de una fuente determinado en la tabla.
            /// </summary>
            /// <returns>Indice de la fuente consultada.</returns>
            public int IndexOf(string name)
            {
                return fonts.IndexOf(name);
            }
        }
    }
}
