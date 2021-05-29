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
 * Class:		RtfLex
 * Description:	Analizador léxico de documentos RTF.
 * ******************************************************************************/

using System;
using System.IO;
using System.Text;

namespace Net.Sgoliver.NRtfTree
{
    namespace Core
    {
        /// <summary>
        /// Analizador léxico (tokenizador) para documentos en formato RTF. Analiza el documento y devuelve de 
        /// forma secuencial todos los elementos RTF leidos (tokens).
        /// </summary>
        public class RtfLex
        {
            #region Atributos privados

            /// <summary>
            /// Fichero abierto.
            /// </summary>
            private TextReader rtf;

            #endregion

            #region Constantes

            /// <summary>
            /// Marca de fin de fichero.
            /// </summary>
            private const int Eof = -1;

            #endregion

            #region Constructores

            /// <summary>
            /// Constructor de la clase RtfLex
            /// </summary>
            /// <param name="rtfReader">Stream del fichero a analizar.</param>
            public RtfLex(TextReader rtfReader)
            {
                rtf = rtfReader;
            }

            #endregion

            #region Métodos Públicos

            /// <summary>
            /// Lee un nuevo token del documento RTF.
            /// </summary>
            /// <returns>Siguiente token leido del documento.</returns>
            public RtfToken NextToken()
            {
                //Caracter leido del documento
                int c;

                //Se crea el nuevo token a devolver
                RtfToken token = new RtfToken();

                //Se lee el siguiente caracter del documento
                c = rtf.Read();

                //Se ignoran los retornos de carro, tabuladores y caracteres nulos
                while (c == '\r' || c == '\n' || c == '\t' || c == '\0')
                    c = rtf.Read();

                //Se trata el caracter leido
                if (c != Eof)
                {
                    switch (c)
                    {
                        case '{':
                            token.Type = RtfTokenType.GroupStart;
                            break;
                        case '}':
                            token.Type = RtfTokenType.GroupEnd;
                            break;
                        case '\\':
                            parseKeyword(token);
                            break;
                        default:
                            token.Type = RtfTokenType.Text;
                            parseText(c, token);
                            break;
                    }
                }
                else
                {
                    //Fin de fichero
                    token.Type = RtfTokenType.Eof;
                }

                return token;
            }


            #endregion

            #region Métodos Privados

            /// <summary>
            /// Lee una palabra clave del documento RTF.
            /// </summary>
            /// <param name="token">Token RTF al que se asignará la palabra clave.</param>
            private void parseKeyword(RtfToken token)
            {
                StringBuilder palabraClave = new StringBuilder();

                StringBuilder parametroStr = new StringBuilder();
                int parametroInt = 0;

                int c;
                bool negativo = false;

                c = rtf.Peek();

                //Si el caracter leido no es una letra --> Se trata de un símbolo de Control o un caracter especial: '\\', '\{' o '\}'
                if (!Char.IsLetter((char)c))
                {
                    rtf.Read();

					if(c == '\\' || c == '{' || c == '}')  //Caracter especial
					{
						token.Type = RtfTokenType.Text;
						token.Key = ((char)c).ToString();
					}
					else   //Simbolo de control
					{
                        token.Type = RtfTokenType.Control;
                        token.Key = ((char)c).ToString();

                        //Si se trata de un caracter especial (codigo de 8 bits) se lee el parámetro hexadecimal
                        if (token.Key == "\'")
                        {
                            string cod = "";

                            cod += (char)rtf.Read();
                            cod += (char)rtf.Read();

                            token.HasParameter = true;

                            token.Parameter = Convert.ToInt32(cod, 16);
                        }

                        //TODO: ¿Hay más símbolos de Control con parámetros?
				    }

                    return;
                }

                //Se lee la palabra clave completa (hasta encontrar un caracter no alfanumérico, por ejemplo '\' ó ' '
                c = rtf.Peek();
                while (Char.IsLetter((char)c))
                {
                    rtf.Read();
                    palabraClave.Append((char)c);

                    c = rtf.Peek();
                }

                //Se asigna la palabra clave leida
                token.Type = RtfTokenType.Keyword;
                token.Key = palabraClave.ToString();

                //Se comprueba si la palabra clave tiene parámetro
                if (Char.IsDigit((char)c) || c == '-')
                {
                    token.HasParameter = true;

                    //Se comprubea si el parámetro es negativo
                    if (c == '-')
                    {
                        negativo = true;

                        rtf.Read();
                    }

                    //Se lee el parámetro completo
                    c = rtf.Peek();
                    while (Char.IsDigit((char)c))
                    {
                        rtf.Read();
                        parametroStr.Append((char)c);

                        c = rtf.Peek();
                    }

                    parametroInt = Convert.ToInt32(parametroStr.ToString());

                    if (negativo)
                        parametroInt = -parametroInt;

                    //Se asigna el parámetro de la palabra clave
                    token.Parameter = parametroInt;
                }

                if (c == ' ')
                {
                    rtf.Read();
                }
            }

            /// <summary>
            /// Lee una cadena de Texto del documento RTF.
            /// </summary>
            /// <param name="car">Primer caracter de la cadena.</param>
            /// <param name="token">Token RTF al que se asignará la palabra clave.</param>
            private void parseText(int car, RtfToken token)
            {
                int c = car;

                StringBuilder Texto = new StringBuilder(((char)c).ToString(),3000000);

                c = rtf.Peek();

                //Se ignoran los retornos de carro, tabuladores y caracteres nulos
                while (c == '\r' || c == '\n' || c == '\t' || c == '\0')
                {
                    rtf.Read();
                    c = rtf.Peek();
                }

                while (c != '\\' && c != '}' && c != '{' && c != Eof)
                {
                    rtf.Read();

                    Texto.Append((char)c);

                    c = rtf.Peek();

                    //Se ignoran los retornos de carro, tabuladores y caracteres nulos
                    while (c == '\r' || c == '\n' || c == '\t' || c == '\0')
                    {
                        rtf.Read();
                        c = rtf.Peek();
                    }
                }

                token.Key = Texto.ToString();
            }

            #endregion
        }
    }
}
