﻿// MIT License
//
// Copyright (c) 2016-2018 Wojciech Nagórski
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

using ExtendedXmlSerializer.Configuration;
using ExtendedXmlSerializer.ContentModel;
using ExtendedXmlSerializer.Core;
using ExtendedXmlSerializer.Core.Sources;
using ExtendedXmlSerializer.Core.Specifications;
using ExtendedXmlSerializer.ExtensionModel.Content;
using ExtendedXmlSerializer.ExtensionModel.Content.Registration;
using ExtendedXmlSerializer.ExtensionModel.Services;
using ExtendedXmlSerializer.ExtensionModel.Types.Sources;
using ExtendedXmlSerializer.ReflectionModel;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ExtendedXmlSerializer.ExtensionModel
{
	public interface IExtend<out T> : IParameterizedSource<IExtensions, T> where T : class, ISerializerExtension { }

	interface IExtensions<out T> : IParameterizedSource<IExtensions, T> where T : ISerializerExtension {}

	sealed class Extensions<T> : IExtensions<T> where T : ISerializerExtension
	{
		public static Extensions<T> Default { get; } = new Extensions<T>();
		Extensions() : this(Support<T>.NewOrSingleton) {}

		readonly Func<T> _create;

		public Extensions(Func<T> create) => _create = create;

		public T Get(IExtensions parameter)
		{
			var existing = parameter.OfType<T>().FirstOrDefault();
			if (existing == null)
			{
				var result = _create();
				parameter.Execute(result);
				return result;
			}

			return existing;
		}
	}

	public class TypeContainerAttribute : Attribute, ISource<Type>
	{
		readonly Type _serializerType;

		public TypeContainerAttribute(Type serializerType) => _serializerType = serializerType;

		public Type Get() => _serializerType;
	}


	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class DependencyAttribute : TypeContainerAttribute
	{
		public DependencyAttribute(Type serializerType) : base(serializerType) {}
	}


	sealed class ExtensionsWithDependencies<T> : IExtensions<T> where T : ISerializerExtension
	{
		public static ExtensionsWithDependencies<T> Default { get; } = new ExtensionsWithDependencies<T>();
		ExtensionsWithDependencies() : this(Extensions<T>.Default.Get,
											DeclaredDependencies<T>.Default
											                       .YieldMetadata()
											                       .Select(Initialize.Default.Get)
											                       .Select(x => x.ToInstanceCommand())
											                       .Fold()) {}

		readonly Func<IExtensions, T> _extensions;
		readonly ICommand<IExtensions> _initializers;

		public ExtensionsWithDependencies(Func<IExtensions, T> extensions, ICommand<IExtensions> initializers)
		{
			_extensions = extensions;
			_initializers = initializers;
		}

		public T Get(IExtensions parameter)
		{
			_initializers.Execute(parameter);
			var result = _extensions(parameter);
			return result;
		}
	}


	sealed class Initialize : Generic<ICommand<IExtensions>>
	{
		public static Initialize Default { get; } = new Initialize();
		Initialize() : base(typeof(Initialize<>)) {}
	}

	sealed class Initialize<T> : ICommand<IExtensions> where T : class, ISerializerExtension
	{
		[UsedImplicitly]
		public static Initialize<T> Default { get; } = new Initialize<T>();
		Initialize() : this(new ConditionalSpecification<IExtensions>(), Extend<T>.Default) { }

		readonly ISpecification<IExtensions> _specification;
		readonly IExtend<T> _extend;

		public Initialize(ISpecification<IExtensions> specification, IExtend<T> extend)
		{
			_specification = specification;
			_extend = extend;
		}

		public void Execute(IExtensions parameter)
		{
			if (_specification.IsSatisfiedBy(parameter))
			{
				_extend.Get(parameter);
			}
		}
	}

	sealed class DeclaredDependencies<T> : Items<Type> where T : ISerializerExtension
	{
		public static DeclaredDependencies<T> Default { get; } = new DeclaredDependencies<T>();
		DeclaredDependencies() : base(TypesFrom<DependencyAttribute>.Default.Get(typeof(T))) {}
	}

	sealed class TypesFrom<T> : IParameterizedSource<MemberInfo, IEnumerable<Type>> where T : Attribute, ISource<Type>
	{
		public static TypesFrom<T> Default { get; } = new TypesFrom<T>();
		TypesFrom() {}

		public IEnumerable<Type> Get(MemberInfo parameter) => parameter.GetCustomAttributes<T>()
		                                                               .Select(x => x.Get());
	}

/*
	sealed class DeclaredDependencies : ItemsBase<Type>
	{
		readonly Type _root;
		readonly Func<MemberInfo, IEnumerable<Type>> _select;

		public DeclaredDependencies(Type root) : this(root, TypesFrom<DependencyAttribute>.Default.Get) {}

		public DeclaredDependencies(Type root, Func<MemberInfo, IEnumerable<Type>> select)
		{
			_root = root;
			_select = @select;
		}

		public override IEnumerator<Type> GetEnumerator() => Select(_select(_root), _ => true).GetEnumerator();

		IEnumerable<Type> Select(IEnumerable<Type> parameter, Func<Type, bool> specification)
		{
			var current = parameter.Fixed();
			var associated = current.SelectMany(_select)
			                        .Where(specification)
			                        .Fixed();
			var updated = current.Union(associated)
			                     .Fixed();
			var others = associated.Any()
				             ? Select(associated, new DelegatedSpecification<Type>(specification).And(new ContainsSpecification<Type>(updated).Inverse())
				                                                                                 .IsSatisfiedBy)
				             : Enumerable.Empty<Type>();
			var result = updated.Union(others);
			return result;
		}
	}
*/


	sealed class Extend<T> : ReferenceCache<IExtensions, T>, IExtend<T> where T : class, ISerializerExtension
	{
		public static Extend<T> Default { get; } = new Extend<T>();
		Extend() : this(ExtensionsWithDependencies<T>.Default.Get) { }

		public Extend(ConditionalWeakTable<IExtensions, T>.CreateValueCallback callback) : base(callback) { }
	}

	public static class ExtensionMethods
	{
		public static IConfigurationElement DiscoverDeclaredContentSerializers<T>(this IConfigurationElement @this)
			=> @this.DiscoverDeclaredContentSerializers(new PublicTypesInSameNamespace<T>());

		public static IConfigurationElement DiscoverDeclaredContentSerializers(this IConfigurationElement @this, IEnumerable<TypeInfo> candidates)
			=> @this.DiscoverDeclaredContentSerializers(new Metadata(candidates));

		public static IConfigurationElement DiscoverDeclaredContentSerializers(
			this IConfigurationElement @this, IEnumerable<MemberInfo> candidates)
			=> @this.Extend<DeclaredMetadataContentExtension>()
			        .Executed(candidates)
			        .Return(@this);

		public static IConfigurationElement EnableThreadProtection(this IConfigurationElement @this)
			=> @this.Extended<ThreadProtectionExtension>();

		public static T EnableRootInstances<T>(this T @this) where T : IConfigurationElement
			=> @this.Extend<RootInstanceExtension>().Return(@this);

		public static MemberInfo Member(this ISource<MemberInfo> @this) => @this.Get();

		public static TypeInfo Type(this ISource<TypeInfo> @this) => @this.Get();

		public static ConfigurationElement<T> Register<T>(this ConfigurationElement<T> @this, ContentModel.ISerializer<T> serializer) =>
			Register(@this, serializer.Adapt());

		public static ConfigurationElement<T> Register<T>(this ConfigurationElement<T> @this, ISerializer serializer)
			=> @this.Register(new ContentSerializerAdapter<T>(serializer.Adapt<T>()));

		public static ConfigurationElement<T> Register<T>(this ConfigurationElement<T> @this, IContentSerializer<T> serializer)
			=> @this.Register(new InstanceService<IContentSerializer<object>>(new GenerializedContentSerializer<T>(serializer)));

		public static ConfigurationElement<T> Register<T>(this ConfigurationElement<T> @this,
		                                                IService<IContentSerializer<object>> service)
			=> @this.Extend<RegisteredSerializersExtension>()
			        .Assign(@this.Type(), service)
			        .Return(@this);

		public static ConfigurationElement<T> Unregister<T>(this ConfigurationElement<T> @this)
			=> @this.Extend<RegisteredSerializersExtension>()
			        .Executed(@this.Type())
			        .Return(@this);

		public static ConfigurationElement<T> Register<T, TSerializer>(this IConfigurationElement @this)
			where TSerializer : ISerializer<T> => @this.Type<T>()
			                                           .Register(typeof(TSerializer));

		public static ConfigurationElement<T> Register<T>(this ConfigurationElement<T> @this, Type serializerType)
			=> @this.Extend<RegisteredSerializersExtension>()
			        .Assign(@this.Type(), serializerType)
			        .Return(@this);

		public static ConfigurationElement<T> Register<T, TSerializer>(this ConfigurationElement<T> @this, A<TSerializer> _)
			where TSerializer : IContentSerializer<T>
			=> @this.Extend<RegisteredSerializersExtension>()
			        .Assign(@this.Type(),
			                A<ActivatedContentSerializer<T, TSerializer>>.Default)
			        .Return(@this);

		public static ConfigurationElement<T, TMember> Register<T, TMember>(this ConfigurationElement<T, TMember> @this,
		                                                                    Type serializerType)
			=> @this.Extend<RegisteredSerializersExtension>()
			        .Assign(@this.Member(), serializerType)
			        .Return(@this);

		public static ConfigurationElement<T, TMember> Register<T, TMember, TSerializer>(this ConfigurationElement<T, TMember> @this, A<TSerializer> _)
			where TSerializer : IContentSerializer<TMember>
			=> @this.Extend<RegisteredSerializersExtension>()
			        .Assign(@this.Member(),
			                A<ActivatedContentSerializer<TMember, TSerializer>>.Default)
			        .Return(@this);

		public static ConfigurationElement<T, TMember> Register<T, TMember>(this ConfigurationElement<T, TMember> @this,
		                                                                    IContentSerializer<TMember> serializer)
			=> @this.Extend<RegisteredSerializersExtension>()
			        .Assign(@this.Member(),
			                new GenerializedContentSerializer<TMember>(serializer))
			        .Return(@this);

		public static ConfigurationElement<T, TMember> Unregister<T, TMember>(this ConfigurationElement<T, TMember> @this)
			=> @this.Extend<RegisteredSerializersExtension>()
			        .Executed(@this.Member())
			        .Return(@this);
	}
}