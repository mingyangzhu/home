// MIT License
// 
// Copyright (c) 2016 Wojciech Nag�rski
//                    Michael DeMond
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Reflection;
using System.Xml;
using ExtendedXmlSerializer.ContentModel.Conversion.Formatting;
using ExtendedXmlSerializer.ContentModel.Conversion.Parsing;
using ExtendedXmlSerializer.ExtensionModel.Xml;

namespace ExtendedXmlSerializer.ContentModel.Xml
{
	sealed class XmlReader : IXmlReader
	{
		readonly static IdentityStore Store = IdentityStore.Default;

		readonly IIdentityStore _store;
		readonly IXmlReaderContext _context;
		readonly System.Xml.XmlReader _reader;

		public XmlReader(IXmlReaderContext context, System.Xml.XmlReader reader) : this(Store, context, reader) {}

		public XmlReader(IIdentityStore store, IXmlReaderContext context, System.Xml.XmlReader reader)
		{
			_store = store;
			_context = context;
			_reader = reader;
		}

		public string Name => _reader.LocalName;
		public string Identifier => _reader.NamespaceURI;

		public MemberInfo Get(string parameter) => _context.Get(parameter);

		public TypeInfo Get(TypeParts parameter) => _context.Get(parameter);

		public override string ToString() => $"{base.ToString()}: {IdentityFormatter.Default.Get(this)}";

		public bool IsSatisfiedBy(IIdentity parameter)
			=> Any() && _reader.MoveToAttribute(parameter.Name, parameter.Identifier);

		public System.Xml.XmlReader Get() => _reader;

		public bool Any() => _reader.HasAttributes;

		public string Content()
		{
			var result = Value();
			Set();
			return result;
		}

		string Value()
		{
			switch (_reader.NodeType)
			{
				case XmlNodeType.Attribute:
					return _reader.Value;
				default:
					_reader.Read();
					var result = _reader.Value;
					_reader.Read();
					return result;
			}
		}

		public void Set()
		{
			/*switch (_reader.NodeType)
			{
				case XmlNodeType.EndElement:
					_reader.ReadEndElement();
					break;
			}*/
			_reader.MoveToContent();
		}

		public IIdentity Get(IIdentity parameter)
			=> _store.Get(parameter.Name, _reader.LookupNamespace(parameter.Identifier));

		public void Dispose()
		{
			_reader.Dispose();
			_context.Dispose();
		}
	}
}