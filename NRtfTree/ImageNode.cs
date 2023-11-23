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

using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using NRtfTree.Core;

namespace NRtfTree
{
    namespace Util
    {
        /// <summary>
        /// Encapsula un nodo RTF de tipo Imagen (Palabra clave "\pict")
        /// </summary>
        public class ImageNode : RtfTreeNode
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
                if (node != null)
                {
                    //Asignamos todos los campos del nodo
                    NodeKey = node.NodeKey;
                    HasParameter = node.HasParameter;
                    Parameter = node.Parameter;
                    ParentNode = node.ParentNode;
                    RootNode = node.RootNode;
                    NodeType = node.NodeType;

                    ChildNodes.Clear();
                    ChildNodes.AddRange(node.ChildNodes);

                    //Obtenemos los datos de la imagen como un array de bytes
                    GetImageData();
                }
            }

            #endregion

            #region Propiedades

            /// <summary>
            /// Devuelve una cadena de caracteres con el contenido de la imagen en formato hexadecimal.
            /// </summary>
            public string HexData => SelectSingleChildNode(RtfNodeType.Text).NodeKey;

            /// <summary>
            /// Devuelve el formato original de la imagen.
            /// </summary>
            public System.Drawing.Imaging.ImageFormat ImageFormat
            {
                get
                {
                    if (SelectSingleChildNode("jpegblip") != null)
                        return System.Drawing.Imaging.ImageFormat.Jpeg;
                    if (SelectSingleChildNode("pngblip") != null)
                        return System.Drawing.Imaging.ImageFormat.Png;
                    if (SelectSingleChildNode("emfblip") != null)
                        return System.Drawing.Imaging.ImageFormat.Emf;
                    if (SelectSingleChildNode("wmetafile") != null)
                        return System.Drawing.Imaging.ImageFormat.Wmf;
                    if (SelectSingleChildNode("dibitmap") != null || SelectSingleChildNode("wbitmap") != null)
                        return System.Drawing.Imaging.ImageFormat.Bmp;
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
                    RtfTreeNode node = SelectSingleChildNode("picw");

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
                    RtfTreeNode node = SelectSingleChildNode("pich");

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
                    RtfTreeNode node = SelectSingleChildNode("picwgoal");

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
                    RtfTreeNode node = SelectSingleChildNode("pichgoal");

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
                    RtfTreeNode node = SelectSingleChildNode("picescalex");

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
                    RtfTreeNode node = SelectSingleChildNode("picescaley");

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
                    MemoryStream stream = new(data, 0, data.Length);

                    //Escribir a un fichero cualquier tipo de imagen
                    Bitmap bitmap = new(stream);
                    bitmap.Save(filePath, ImageFormat);
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
                    MemoryStream stream = new(data, 0, data.Length);

                    //System.Drawing.Imaging.Metafile metafile = new System.Drawing.Imaging.Metafile(stream);

                    //Escribir directamente el array de bytes a un fichero ".jpg"
                    //FileStream fs = new FileStream("c:\\prueba.jpg", FileMode.CreateNew);
                    //BinaryWriter w = new BinaryWriter(fs);
                    //w.Write(image,0,imageSize);
                    //w.Close();
                    //fs.Close();

                    //Escribir a un fichero cualquier tipo de imagen
                    Bitmap bitmap = new(stream);
                    bitmap.Save(filePath, format);
                }
            }

            #endregion

            #region Metodos privados

            /// <summary>
            /// Obtiene los datos de la imagen a partir de la información contenida en el nodo RTF.
            /// </summary>
            private void GetImageData()
            {
                //Formato 1 (Word 97-2000): {\*\shppict {\pict\jpegblip <datos>}}{\nonshppict {\pict\wmetafile8 <datos>}}
                //Formato 2 (Wordpad)     : {\pict\wmetafile8 <datos>}

                string Text;

                if (FirstChild.NodeKey == "pict")
                {
                    Text = SelectSingleChildNode(RtfNodeType.Text).NodeKey;

                    int dataSize = Text.Length / 2;
                    data = new byte[dataSize];

                    StringBuilder sbaux = new(2);

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
