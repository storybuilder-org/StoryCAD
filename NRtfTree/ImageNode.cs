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
 * Class:		ImageNode
 * Description:	Nodo RTF especializado que contiene la información de una imagen.
 * ******************************************************************************/

using System.Text;
using Net.Sgoliver.NRtfTree.Core;
using System.IO;
using System.Globalization;
using System.Drawing;

namespace Net.Sgoliver.NRtfTree
{
    namespace Util
    {
        /// <summary>
        /// Encapsula un nodo RTF de tipo Imagen (Palabra clave "\pict")
        /// </summary>
        public class ImageNode : Net.Sgoliver.NRtfTree.Core.RtfTreeNode
        {
            #region Atributos privados

            /// <summary>
            /// Array de bytes con la información de la imagen.
            /// </summary>
            private byte[] data;

            #endregion

            #region Constructores

            /// <summary>
            /// Constructor de la clase ImageNode.
            /// </summary>
            /// <param name="node">Nodo RTF del que se obtendrán los datos de la imagen.</param>
            public ImageNode(RtfTreeNode node)
            {
				if(node != null)
				{
					//Asignamos todos los campos del nodo
					this.NodeKey = node.NodeKey;
					this.HasParameter = node.HasParameter;
					this.Parameter = node.Parameter;
					this.ParentNode = node.ParentNode;
					this.RootNode = node.RootNode;
					this.NodeType = node.NodeType;

					this.ChildNodes.Clear();
					this.ChildNodes.AddRange(node.ChildNodes);

					//Obtenemos los datos de la imagen como un array de bytes
					getImageData();
				}
            }

            #endregion

            #region Propiedades

			/// <summary>
			/// Devuelve una cadena de caracteres con el contenido de la imagen en formato hexadecimal.
			/// </summary>
			public string HexData
			{
				get
				{
					return this.SelectSingleChildNode(RtfNodeType.Text).NodeKey;
				}
			}

            /// <summary>
            /// Devuelve el formato original de la imagen.
            /// </summary>
            public System.Drawing.Imaging.ImageFormat ImageFormat
            { 
                get 
                {
                    if (this.SelectSingleChildNode("jpegblip") != null)
                        return System.Drawing.Imaging.ImageFormat.Jpeg;
                    else if (this.SelectSingleChildNode("pngblip") != null)
                        return System.Drawing.Imaging.ImageFormat.Png;
                    else if (this.SelectSingleChildNode("emfblip") != null)
                        return System.Drawing.Imaging.ImageFormat.Emf;
                    else if (this.SelectSingleChildNode("wmetafile") != null)
                        return System.Drawing.Imaging.ImageFormat.Wmf;
                    else if (this.SelectSingleChildNode("dibitmap") != null || this.SelectSingleChildNode("wbitmap") != null)
                        return System.Drawing.Imaging.ImageFormat.Bmp;
                    else
                        return null;
                }
            }

            /// <summary>
            /// Devuelve el ancho de la imagen (en twips).
            /// </summary>
            public int Width
            {
                get
                {
                    RtfTreeNode node = this.SelectSingleChildNode("picw");

                    if (node != null)
                        return node.Parameter;
                    else
                        return -1;
                }
            }

            /// <summary>
            /// Devuelve el alto de la imagen (en twips).
            /// </summary> 
            public int Height
            {
                get
                {
                    RtfTreeNode node = this.SelectSingleChildNode("pich");

                    if (node != null)
                        return node.Parameter;
                    else
                        return -1;
                }
            }

            /// <summary>
            /// Devuelve el ancho objetivo de la imagen (en twips).
            /// </summary>
            public int DesiredWidth
            {
                get
                {
                    RtfTreeNode node = this.SelectSingleChildNode("picwgoal");

                    if (node != null)
                        return node.Parameter;
                    else
                        return -1;
                }
            }

            /// <summary>
            /// Devuelve el alto objetivo de la imagen (en twips).
            /// </summary>
            public int DesiredHeight
            {
                get
                {
                    RtfTreeNode node = this.SelectSingleChildNode("pichgoal");

                    if (node != null)
                        return node.Parameter;
                    else
                        return -1;
                }
            }

            /// <summary>
            /// Devuelve la escala horizontal de la imagen, en porcentaje.
            /// </summary>
            public int ScaleX
            {
                get
                {
                    RtfTreeNode node = this.SelectSingleChildNode("picescalex");

                    if (node != null)
                        return node.Parameter;
                    else
                        return -1;
                }
            }

            /// <summary>
            /// Devuelve la escala vertical de la imagen, en porcentaje.
            /// </summary>
            public int ScaleY
            {
                get
                {
                    RtfTreeNode node = this.SelectSingleChildNode("picescaley");

                    if (node != null)
                        return node.Parameter;
                    else
                        return -1;
                }
            }

            #endregion

            #region Metodos Publicos

			/// <summary>
			/// Devuelve un array de bytes con el contenido de la imagen.
			/// </summary>
			/// <return>Array de bytes con el contenido de la imagen.</return>
			public byte[] GetByteData()
			{
				return data;
			}

            /// <summary>
            /// Guarda una imagen a fichero con el formato original.
            /// </summary>
            /// <param name="filePath">Ruta del fichero donde se guardará la imagen.</param>
            public void SaveImage(string filePath)
            {
                if (data != null)
                {
                    MemoryStream stream = new MemoryStream(data, 0, data.Length);

                    //Escribir a un fichero cualquier tipo de imagen
                    Bitmap bitmap = new Bitmap(stream);
                    bitmap.Save(filePath, this.ImageFormat);
                }
            }

            /// <summary>
            /// Guarda una imagen a fichero con un formato determinado indicado como parámetro.
            /// </summary>
            /// <param name="filePath">Ruta del fichero donde se guardará la imagen.</param>
            /// <param name="format">Formato con el que se escribirá la imagen.</param>
            public void SaveImage(string filePath, System.Drawing.Imaging.ImageFormat format)
            {
                if (data != null)
                {
                    MemoryStream stream = new MemoryStream(data, 0, data.Length);

                    //System.Drawing.Imaging.Metafile metafile = new System.Drawing.Imaging.Metafile(stream);

                    //Escribir directamente el array de bytes a un fichero ".jpg"
                    //FileStream fs = new FileStream("c:\\prueba.jpg", FileMode.CreateNew);
                    //BinaryWriter w = new BinaryWriter(fs);
                    //w.Write(image,0,imageSize);
                    //w.Close();
                    //fs.Close();

                    //Escribir a un fichero cualquier tipo de imagen
                    Bitmap bitmap = new Bitmap(stream);
                    bitmap.Save(filePath, format);
                }
            }

            #endregion

            #region Metodos privados

            /// <summary>
            /// Obtiene los datos de la imagen a partir de la información contenida en el nodo RTF.
            /// </summary>
            private void getImageData()
            {
                //Formato 1 (Word 97-2000): {\*\shppict {\pict\jpegblip <datos>}}{\nonshppict {\pict\wmetafile8 <datos>}}
                //Formato 2 (Wordpad)     : {\pict\wmetafile8 <datos>}

                string Text = "";

                if (this.FirstChild.NodeKey == "pict")
                {
                    Text = this.SelectSingleChildNode(RtfNodeType.Text).NodeKey;

                    int dataSize = Text.Length / 2;
                    data = new byte[dataSize];

                    StringBuilder sbaux = new StringBuilder(2);

                    for (int i = 0; i < Text.Length; i++)
                    {
                        sbaux.Append(Text[i]);

                        if (sbaux.Length == 2)
                        {
                            data[i / 2] = byte.Parse(sbaux.ToString(), NumberStyles.HexNumber);
                            sbaux.Remove(0, 2);
                        }
                    }
                }
            }

            #endregion
        }
    }
}
