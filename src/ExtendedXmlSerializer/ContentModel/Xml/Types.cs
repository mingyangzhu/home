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
using ExtendedXmlSerializer.ContentModel.Formatting;
using ExtendedXmlSerializer.Core.Sources;
using ExtendedXmlSerializer.Core.Specifications;
using ExtendedXmlSerializer.TypeModel;
using JetBrains.Annotations;

namespace ExtendedXmlSerializer.ContentModel.Xml
{
	sealed class Types : ReferenceCacheBase<IIdentity, TypeInfo>, ITypes
	{
		readonly ITypeIdentities _aliased;

		readonly ITypes _known, _partitions;

		[UsedImplicitly]
		public Types(IPartitionedTypeSpecification specification, IAssemblyTypePartitions partitions,
		             ITypeIdentities identities,
		             ITypeFormatter formatter)
			: this(specification, identities, formatter, TypeLoader.Default, partitions) {}

		internal Types(ISpecification<TypeInfo> specification, ITypeIdentities identities, ITypeFormatter formatter,
		               params ITypePartitions[] partitions)
			: this(identities, new IdentityPartitionedTypes(specification, formatter), new PartitionedTypes(partitions)) {}

		Types(ITypeIdentities aliased, ITypes known, ITypes partitions) // : base(IdentityComparer.Default)
		{
			_aliased = aliased;
			_known = known;
			_partitions = partitions;
		}

		protected override TypeInfo Create(IIdentity parameter)
			=> _aliased.Get(parameter) ?? _known.Get(parameter) ?? _partitions.Get(parameter);
	}
}