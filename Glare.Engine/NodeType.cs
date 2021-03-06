﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Glare.Engine {
	/// <summary>Gives information about the <see cref="System.Type"/> of a <see cref="Node"/> class.</summary>
	public class NodeType : NodeMemberInfo {
		static readonly Dictionary<Type, NodeType> NodeTypes = new Dictionary<Type, NodeType>();

		readonly Dictionary<string, NodeTypeProperty> propertiesByName = new Dictionary<string, NodeTypeProperty>();
		readonly Type type;

		/// <summary>Get the system type that this corresponds to.</summary>
		public Type Type { get { return type; } }

		/// <summary>Get the <see cref="NodeType"/> for a <see cref="System.Type"/>.</summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static NodeType GetNodeType(Type type) {
			NodeType nodeType;

			if (NodeTypes.TryGetValue(type, out nodeType))
				return nodeType;

			if (type == null)
				throw new ArgumentNullException("type");
			if (!type.IsSubclassOf(typeof(Node)))
				throw new ArgumentException("Type " + type.Name + " is not a sub-class of " + typeof(Node).Name);

			return NodeTypes[type] = nodeType = new NodeType(type);
		}

		NodeType(Type type)
			: base(type) {
			this.type = type;
		}

		/// <summary>Get a property of the type, or throw an exception.</summary>
		/// <param name="name"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">This <see cref="NodeType"/> does not have an instance public or private property named <paramref name="name"/>.</exception>
		public NodeTypeProperty GetProperty(string name) {
			NodeTypeProperty property;

			if (propertiesByName.TryGetValue(name, out property))
				return property;

			PropertyInfo info = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (info == null)
				throw new ArgumentException("Type " + Name + " does not have a property named " + name + ".");

			return propertiesByName[name] = new NodeTypeProperty(this, info);
		}
	}
}
