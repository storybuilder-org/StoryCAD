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
 * Class:		ObjectNode
 * Description:	Nodo RTF especializado que contiene la información de un objeto.
 * ******************************************************************************/

using System;
using System.Text;
using Net.Sgoliver.NRtfTree.Core;
using System.Globalization;

namespace Net.Sgoliver.NRtfTree
{
    namespace Util
    {
        /// <summary>
        /// Encapsula un nodo RTF de tipo Objeto (Palabra clave "\object")
        /// </summary>
        public class ObjectNode : Net.Sgoliver.NRtfTree.Core.RtfTreeNode
        {
            #region Atributos Privados

            /// <summary>
            /// Array de bytes con la información del objeto.
            /// </summary>
            private byte[] objdata;

            #endregion

            #region Constructores

            /// <summary>
            /// Constructor de la clase ObjectNode.
            /// </summary>
            /// <param name="node">Nodo RTF del que se obtendrán los datos de la imagen.</param>
            public ObjectNode(RtfTreeNode node)
            {
				if(node != null)
				{
					//Asignamos todos los campos del nodo
					this.NodeKey = node.NodeKey;
					this.HasParameter = node.HasParameter;
					this.Parameter= node.Parameter;
					this.ParentNode = node.ParentNode;
					this.RootNode = node.RootNode;
					this.NodeType = node.NodeType;

					this.ChildNodes.Clear();
					this.ChildNodes.AddRange(node.ChildNodes);

					//Obtenemos los datos del objeto como un array de bytes
					getObjectData();
				}
            }

            #endregion

            #region Propiedades

            /// <summary>
            /// Devuelve el tipo de objeto.
            /// </summary>
            public string ObjectType
            {
                get 
                {
                    if (this.SelectSingleChildNode("objemb") != null)
                        return "objemb";
                    if (this.SelectSingleChildNode("objlink") != null)
                        return "objlink";
                    if (this.SelectSingleChildNode("objautlink") != null)
                        return "objautlink";
                    if (this.SelectSingleChildNode("objsub") != null)
                        return "objsub";
                    if (this.SelectSingleChildNode("objpub") != null)
                        return "objpub";
                    if (this.SelectSingleChildNode("objicemb") != null)
                        return "objicemb";
                    if (this.SelectSingleChildNode("objhtml") != null)
                        return "objhtml";
                    if (this.SelectSingleChildNode("objocx") != null)
                        return "objocx";
                    else
                        return "";
                }
            }

            /// <summary>
            /// Devuelve la clase del objeto.
            /// </summary>
            public string ObjectClass
            {
                get
                {
                    //Formato: {\*\objclass Paint.Picture}

                    RtfTreeNode node = this.SelectSingleNode("objclass");

                    if (node != null)
                        return node.NextSibling.NodeKey;
                    else
                        return "";
                }
            }

            /// <summary>
            /// Devuelve el grupo RTF que encapsula el nodo "\result" del objeto.
            /// </summary>
            public RtfTreeNode ResultNode
            {
                get
                {
                    RtfTreeNode node = this.SelectSingleNode("result");

                    //Si existe el nodo "\result" recuperamos el grupo RTF superior.
                    if (node != null)
                        node = node.ParentNode;

                    return node;
                }
            }

			/// <summary>
			/// Devuelve una cadena de caracteres con el contenido del objeto en formato hexadecimal.
			/// </summary>
			public string HexData
			{
				get
				{
					string Text = "";

					//Buscamos el nodo "\objdata"
					RtfTreeNode objdataNode = this.SelectSingleNode("objdata");

					//Si existe el nodo
					if (objdataNode != null)
					{
						//Buscamos los datos en formato hexadecimal (último hijo del grupo de \objdata)
						Text = objdataNode.ParentNode.LastChild.NodeKey;
					}

					return Text;				
				}
			}

            #endregion

			#region Métodos Publicos

			/// <summary>
			/// Devuelve un array de bytes con el contenido del objeto.
			/// </summary>
			/// <returns>Array de bytes con el contenido del objeto.</returns>
			public byte[] GetByteData()
			{
				return objdata;
			}

			#endregion

            #region Métodos Privados

            /// <summary>
            /// Obtiene los datos binarios del objeto a partir de la información contenida en el nodo RTF.
            /// </summary>
            private void getObjectData()
            {
                //Formato: ( '{' \object (<objtype> & <objmod>? & <objclass>? & <objname>? & <objtime>? & <objsize>? & <rsltmod>?) ('{\*' \objdata (<objalias>? & <objsect>?) <data> '}') <result> '}' )

                string Text = "";

                if (this.FirstChild.NodeKey == "object")
                {
                    //Buscamos el nodo "\objdata"
                    RtfTreeNode objdataNode = this.SelectSingleNode("objdata");

                    //Si existe el nodo
                    if (objdataNode != null)
                    {
                        //Buscamos los datos en formato hexadecimal (último hijo del grupo de \objdata)
                        Text = objdataNode.ParentNode.LastChild.NodeKey;

                        int dataSize = Text.Length / 2;
                        objdata = new byte[dataSize];

                        StringBuilder sbaux = new StringBuilder(2);

                        for (int i = 0; i < Text.Length; i++)
                        {
                            sbaux.Append(Text[i]);

                            if (sbaux.Length == 2)
                            {
                                objdata[i / 2] = byte.Parse(sbaux.ToString(), NumberStyles.HexNumber);
                                sbaux.Remove(0, 2);
                            }
                        }
                    }
                }
            }

            #endregion
        }
    }
}
