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
 * Class:		RtfDocument
 * Description:	Clase para la generación de documentos RTF.
 * ******************************************************************************/

using System;
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
        /// Clase para la generación de documentos RTF.
        /// </summary>
        public class RtfDocument
        {
            #region Atributos privados

            /// <summary>
            /// Ruta del fichero a generar.
            /// </summary>
            private string path;

            /// <summary>
            /// Codificación del documento.
            /// </summary>
            private Encoding encoding;

            /// <summary>
            /// Tabla de fuentes del documento.
            /// </summary>
            private RtfFontTable fontTable;

            /// <summary>
            /// Tabla de colores del documento.
            /// </summary>
            private RtfColorTable colorTable;

            /// <summary>
            /// Árbol RTF del documento.
            /// </summary>
            public RtfTree Tree;

            /// <summary>
            /// Grupo principal del documento.
            /// </summary>
            private RtfTreeNode mainGroup;

            /// <summary>
            /// Formato actual del texto.
            /// </summary>
            private RtfTextFormat currentFormat;

            #endregion

            #region Constructores

            /// <summary>
            /// Constructor de la clase RtfDocument.
            /// </summary>
            /// <param name="path">Ruta del fichero a generar.</param>
            /// <param name="enc">Codificación del documento a generar.</param>
            public RtfDocument(string path, Encoding enc)
            {
                this.path = path;
                encoding = enc;

                fontTable = new RtfFontTable();
                fontTable.AddFont("Arial");  //Default font

                colorTable = new RtfColorTable();
                colorTable.AddColor(Color.Black);  //Default color

                currentFormat = null;

                Tree = new RtfTree();
                mainGroup = new RtfTreeNode(RtfNodeType.Group);

                InitializeTree();
            }

            /// <summary>
            /// Constructor de la clase RtfDocument. Se utilizará la codificación por defecto del sistema.
            /// </summary>
            /// <param name="path">Ruta del fichero a generar.</param>
            public RtfDocument(string path) : this(path, Encoding.Default)
            {

            }

            #endregion

            #region Metodos Publicos

            /// <summary>
            /// Cierra el documento RTF.
            /// </summary>
            public void Close()
            {
                InsertFontTable();
                InsertColorTable();
                InsertGenerator();

                mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "par", false, 0));
                Tree.RootNode.AppendChild(mainGroup);

                Tree.SaveRtf(path);
            }

            public string GetRtf()
            {
                InsertFontTable();
                InsertColorTable();
                InsertGenerator();

                mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "par", false, 0));
                Tree.RootNode.AppendChild(mainGroup);

                return Tree.RootNode.Rtf;
            }

            /// <summary>
            /// Inserta un fragmento de texto en el documento con un formato de texto determinado.
            /// </summary>
            /// <param name="text">Texto a insertar.</param>
            /// <param name="format">Formato del texto a insertar.</param>
            public void AddText(string text, RtfTextFormat format)
            {
                UpdateFontTable(format);
                UpdateColorTable(format);

                InsertFormat(format);

                InsertText(text);
            }

            /// <summary>
            /// Inserta un fragmento de texto en el documento con el formato de texto actual.
            /// </summary>
            /// <param name="text">Texto a insertar.</param>
            public void AddText(string text)
            {
                InsertText(text);
            }

            /// <summary>
            /// Inserta un salto de línea en el documento.
            /// </summary>
            public void AddNewLine()
            {
                mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "par", false, 0));
            }

            /// <summary>
            /// Inserta una imagen en el documento.
            /// </summary>
            /// <param name="path">Ruta de la imagen a insertar.</param>
            /// <param name="width">Ancho deseado de la imagen en el documento.</param>
            /// <param name="height">Alto deseado de la imagen en el documento.</param>
            public void AddImage(string path, int width, int height)
            {
                FileStream fStream = null;
                BinaryReader br = null;

                try
                {
                    byte[] data;

                    FileInfo fInfo = new(path);
                    long numBytes = fInfo.Length;

                    fStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    br = new BinaryReader(fStream);

                    data = br.ReadBytes((int)numBytes);

                    StringBuilder hexdata = new ();

                    foreach (byte t in data) { hexdata.Append(GetHexa(t)); }

                    Image img = Image.FromFile(path);

                    RtfTreeNode imgGroup = new(RtfNodeType.Group);
                    imgGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "pict", false, 0));

                    string format;
                    if (path.ToLower().EndsWith("wmf")) {format = "emfblip";}
                    else { format = "jpegblip"; }

                    imgGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, format, false, 0));


                    imgGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "picw", true, img.Width * 20));
                    imgGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "pich", true, img.Height * 20));
                    imgGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "picwgoal", true, width * 20));
                    imgGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "pichgoal", true, height * 20));
                    imgGroup.AppendChild(new RtfTreeNode(RtfNodeType.Text, hexdata.ToString(), false, 0));

                    mainGroup.AppendChild(imgGroup);
                }
                finally
                {
                    br.Close();
                    fStream.Close();
                }
            }

            #endregion

            #region Metodos Privados

            /// <summary>
            /// Obtiene el código hexadecimal de un entero.
            /// </summary>
            /// <param name="code">Número entero.</param>
            /// <returns>Código hexadecimal del entero pasado como parámetro.</returns>
            private string GetHexa(byte code)
            {
                string hexa = Convert.ToString(code, 16);

                if (hexa.Length == 1)
                {
                    hexa = "0" + hexa;
                }

                return hexa;
            }

            /// <summary>
            /// Inserta el código RTF de la tabla de fuentes en el documento.
            /// </summary>
            private void InsertFontTable()
            {
                RtfTreeNode ftGroup = new(RtfNodeType.Group);

                ftGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "fonttbl", false, 0));

                for (int i = 0; i < fontTable.Count; i++)
                {
                    RtfTreeNode ftFont = new(RtfNodeType.Group);
                    ftFont.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "f", true, i));
                    ftFont.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "fnil", false, 0));
                    ftFont.AppendChild(new RtfTreeNode(RtfNodeType.Text, fontTable[i] + ";", false, 0));

                    ftGroup.AppendChild(ftFont);
                }

                mainGroup.InsertChild(5, ftGroup);
            }

            /// <summary>
            /// Inserta el código RTF de la tabla de colores en el documento.
            /// </summary>
            private void InsertColorTable()
            {
                RtfTreeNode ctGroup = new(RtfNodeType.Group);

                ctGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "colortbl", false, 0));

                for (int i = 0; i < colorTable.Count; i++)
                {
                    ctGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "red", true, colorTable[i].R));
                    ctGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "green", true, colorTable[i].G));
                    ctGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "blue", true, colorTable[i].B));
                    ctGroup.AppendChild(new RtfTreeNode(RtfNodeType.Text, ";", false, 0));
                }

                mainGroup.InsertChild(6, ctGroup);
            }

            /// <summary>
            /// Inserta el código RTF de la aplicación generadora del documento.
            /// </summary>
            private void InsertGenerator()
            {
                RtfTreeNode genGroup = new(RtfNodeType.Group);

                genGroup.AppendChild(new RtfTreeNode(RtfNodeType.Control, "*", false, 0));
                genGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "generator", false, 0));
                genGroup.AppendChild(new RtfTreeNode(RtfNodeType.Text, "NRtfTree Library 1.3.0;", false, 0));

                mainGroup.InsertChild(7, genGroup);
            }

            /// <summary>
            /// Inserta todos los nodos de texto y control necesarios para representar un texto determinado.
            /// </summary>
            /// <param name="text">Texto a insertar.</param>
            private void InsertText(string text)
            {
                int i = 0;

                while (i < text.Length)
                {
                    int code = char.ConvertToUtf32(text, i);

                    if (code is >= 32 and < 128)
                    {
                        StringBuilder s = new("");

                        while (i < text.Length && code is >= 32 and < 128)
                        {
                            s.Append(text[i]);

                            i++;

                            if (i < text.Length)
                                code = char.ConvertToUtf32(text, i);
                        }

                        mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Text, s.ToString(), false, 0));
                    }
                    else
                    {
                        byte[] bytes = encoding.GetBytes(new[] { text[i] });

                        mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Control, "'", true, bytes[0]));

                        i++;
                    }
                }
            }

            /// <summary>
            /// Actualiza la tabla de fuentes con una nueva fuente si es necesario.
            /// </summary>
            /// <param name="format"></param>
            private void UpdateFontTable(RtfTextFormat format)
            {
                if (fontTable.IndexOf(format.font) == -1)
                {
                    fontTable.AddFont(format.font);
                }
            }

            /// <summary>
            /// Actualiza la tabla de colores con un nuevo color si es necesario.
            /// </summary>
            /// <param name="format"></param>
            private void UpdateColorTable(RtfTextFormat format)
            {
                if (colorTable.IndexOf(format.color) == -1)
                {
                    colorTable.AddColor(format.color);
                }
            }

            /// <summary>
            /// Inserta las claves RTF necesarias para representar el formato de texto pasado como parámetro.
            /// </summary>
            /// <param name="format">Formato de texto a representar.</param>
            private void InsertFormat(RtfTextFormat format)
            {
                if (currentFormat != null)
                {
                    //Font Color
                    if (format.color.ToArgb() != currentFormat.color.ToArgb())
                    {
                        currentFormat.color = format.color;

                        mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "cf", true, colorTable.IndexOf(format.color)));
                    }

                    //Font Name
                    if (format.size != currentFormat.size)
                    {
                        currentFormat.size = format.size;

                        mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "fs", true, format.size * 2));
                    }

                    //Font Size
                    if (format.font != currentFormat.font)
                    {
                        currentFormat.font = format.font;

                        mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "f", true, fontTable.IndexOf(format.font)));
                    }

                    //Bold
                    if (format.bold != currentFormat.bold)
                    {
                        currentFormat.bold = format.bold;

                        mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "b", !format.bold, 0));
                    }

                    //Italic
                    if (format.italic != currentFormat.italic)
                    {
                        currentFormat.italic = format.italic;

                        mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "i", !format.italic, 0));
                    }

                    //Underline
                    if (format.underline != currentFormat.underline)
                    {
                        currentFormat.underline = format.underline;

                        mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "ul", !format.underline, 0));
                    }
                }
                else //currentFormat == null
                {
                    mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "cf", true, colorTable.IndexOf(format.color)));
                    mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "fs", true, format.size * 2));
                    mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "f", true, fontTable.IndexOf(format.font)));

                    if (format.bold)
                        mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "b", false, 0));

                    if (format.italic)
                        mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "i", false, 0));

                    if (format.underline)
                        mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "ul", false, 0));

                    currentFormat = new RtfTextFormat();
                    currentFormat.color = format.color;
                    currentFormat.size = format.size;
                    currentFormat.font = format.font;
                    currentFormat.bold = format.bold;
                    currentFormat.italic = format.italic;
                    currentFormat.underline = format.underline;
                }
            }

            /// <summary>
            /// Inicializa el arbol RTF con todas las claves de la cabecera del documento.
            /// </summary>
            private void InitializeTree()
            {
                mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "rtf", true, 1));
                mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "ansi", false, 0));
                mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "ansicpg", true, encoding.CodePage));
                mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "deff", true, 0));
                mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "deflang", true, CultureInfo.CurrentCulture.LCID));

                mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "viewkind", true, 4));
                mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "uc", true, 1));
                mainGroup.AppendChild(new RtfTreeNode(RtfNodeType.Keyword, "pard", false, 0));
            }

            #endregion
        }
    }
}
