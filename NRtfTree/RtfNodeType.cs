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
 * Class:		RtfNodeType
 * Description:	Tipos de nodo de un árbol RTF.
 * ******************************************************************************/

using System;

namespace Net.Sgoliver.NRtfTree
{
    namespace Core
    {
        /// <summary>
        /// Tipos de nodo de un documento RTF.
        /// </summary>
        public enum RtfNodeType
        {
            /// <summary>
            /// Nodo raíz.
            /// </summary>
            Root = 0,
            /// <summary>
            /// Palabra clave.
            /// </summary>
            Keyword = 1,
            /// <summary>
            /// Símbolo de Control.
            /// </summary>
            Control = 2,
            /// <summary>
            /// Texto del documento.
            /// </summary>
            Text = 3,
            /// <summary>
            /// Grupo RTF
            /// </summary>
            Group = 4,
            /// <summary>
            /// No se ha inicializado el nodo
            /// </summary>
            None = 5
        }
    }
}
