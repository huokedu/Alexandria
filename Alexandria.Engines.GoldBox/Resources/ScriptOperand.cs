﻿using Glare.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alexandria.Engines.GoldBox.Resources {
	/// <summary>An operand to a <see cref="ScriptInstruction"/>.</summary>
	public class ScriptOperand {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool valid = true;

		ScriptOperandType Type;
		readonly int CoreIntegerValue;
		readonly string CoreStringValue;

		/// <summary></summary>
		public ScriptInstruction Instruction { get; internal set; }

		/// <summary></summary>
		public Script Script { get { return Instruction != null ? Instruction.Script : null; } }

		/// <summary></summary>
		public ScriptInstruction Target { get; internal set; }

		void RequireType(ScriptOperandType type) { if (Type != type) throw new InvalidOperationException("This is not a " + type + " " + typeof(ScriptOperand).Name + "; it is a " + Type + "."); }

		/// <summary></summary>
		public int AsAddress { get { if (!IsAddress) throw new InvalidOperationException("This is not an address token."); return CoreIntegerValue; } }
		
		/// <summary></summary>
		public ScriptComparison AsComparison { get { RequireType(ScriptOperandType.Comparison); return (ScriptComparison)CoreIntegerValue; } }

		/// <summary></summary>
		public ScriptOpcode AsOpcode { get { if (!IsOpcode) throw new InvalidOperationException("This is not an opcode token."); return (ScriptOpcode)CoreIntegerValue; } }

		/// <summary></summary>
		public string AsString { get { if (!IsString) throw new InvalidOperationException("This is not a string token."); return CoreStringValue; } }

		/// <summary></summary>
		public int AsStringVariable { get { if (!IsStringVariable) throw new InvalidOperationException("This is not a string variable token."); return CoreIntegerValue; } }

		/// <summary></summary>
		public int AsUnknownVariable { get { RequireType(ScriptOperandType.UnknownVariable); return CoreIntegerValue; } }

		/// <summary></summary>
		public byte AsValue { get { RequireType(ScriptOperandType.Value); return (byte)CoreIntegerValue; } }

		/// <summary></summary>
		public int AsVariable { get { if (!IsVariable) throw new InvalidOperationException("This is not a variable."); return CoreIntegerValue; } }

		/// <summary></summary>
		public int? TryAddress { get { return IsAddress ? (int?)CoreIntegerValue : null; } }

		/// <summary></summary>
		public ScriptComparison? TryComparison { get { return IsComparison ? (ScriptComparison?)CoreIntegerValue : null; } }

		/// <summary></summary>
		public ScriptOpcode? TryOpcode { get { return IsOpcode ? (ScriptOpcode?)CoreIntegerValue : null; } }

		/// <summary></summary>
		public string TryString { get { return IsString ? CoreStringValue : null; } }

		/// <summary></summary>
		public int? TryStringVariable { get { return IsStringVariable ? (int?)CoreIntegerValue : null; } }

		/// <summary></summary>
		public int? TryUnknownVariable { get { return IsUnknownVariable ? (int?)CoreIntegerValue : null; } }

		/// <summary></summary>
		public byte? TryValue { get { return IsValue ? (byte?)CoreIntegerValue : null; } }

		/// <summary></summary>
		public int? TryVariable { get { return IsVariable ? (int?)CoreIntegerValue : null; } }

		/// <summary></summary>
		public bool IsAddress { get { return Type == ScriptOperandType.Address; } }

		/// <summary></summary>
		public bool IsComparison { get { return Type == ScriptOperandType.Comparison; } }

		/// <summary></summary>
		public bool IsComparisonDefined { get { return IsComparison && typeof(ScriptComparison).IsEnumDefined((ScriptComparison)CoreIntegerValue); } }

		/// <summary></summary>
		public bool IsOpcode { get { return Type == ScriptOperandType.Opcode; } }

		/// <summary></summary>
		public bool IsString { get { return Type == ScriptOperandType.String; } }

		/// <summary></summary>
		public bool IsStringVariable { get { return Type == ScriptOperandType.StringVariable; } }

		/// <summary></summary>
		public bool IsUnknownVariable { get { return Type == ScriptOperandType.UnknownVariable; } }

		/// <summary></summary>
		public bool IsValue { get { return Type == ScriptOperandType.Value; } }

		/// <summary></summary>
		public bool IsVariable { get { return Type == ScriptOperandType.Variable; } }

		/// <summary></summary>
		public bool IsValid { get { return valid; } internal set { valid = value; } }

		/// <summary></summary>
		public static readonly ScriptOperand Empty = new ScriptOperand();

		ScriptOperand() { }

		/// <summary></summary>
		public ScriptOperand(byte value) { Type = ScriptOperandType.Value; CoreIntegerValue = value; }

		/// <summary></summary>
		public ScriptOperand(BinaryReader reader, ScriptArgument expected) {
			ScriptOperandType value = (ScriptOperandType)reader.ReadByte();

			// Expectations can transform the result due to overlapping in coding, so deal with them here.
			switch (expected) {
				case ScriptArgument.Literal: // Single code byte, any value
					Type = ScriptOperandType.Value;
					CoreIntegerValue = (byte)value;
					return;

				case ScriptArgument.Opcode: // Single byte, any value
					if (value == ScriptOperandType.Value || value == ScriptOperandType.Variable || value == ScriptOperandType.UnknownVariable) {
						Type = ScriptOperandType.Opcode;
						CoreIntegerValue = (byte)value;
						return;
					}
					break;

				case ScriptArgument.Address: // 0x01
					if (value == ScriptOperandType.Variable) {
						Type = ScriptOperandType.Address;
						CoreIntegerValue = reader.ReadUInt16();
						return;
					}
					break;

				case ScriptArgument.Comparison: // Single code byte
					Type = ScriptOperandType.Comparison;
					CoreIntegerValue = (byte)value;
					return;

				default:
					break;
			}

			// Now we can freely handle the data.
			switch (value) {
				case ScriptOperandType.Value: // 0x00
					Type = ScriptOperandType.Value;
					CoreIntegerValue = reader.ReadByte();
					break;

				case ScriptOperandType.Variable: // 0x01
					Type = ScriptOperandType.Variable;
					CoreIntegerValue = reader.ReadUInt16();
					break;

				case ScriptOperandType.UnknownVariable: // 0x03
					Type = ScriptOperandType.UnknownVariable;
					CoreIntegerValue = reader.ReadUInt16();
					break;

				case ScriptOperandType.String: // 0x80
					Type = ScriptOperandType.String;
					byte stringLength = reader.ReadByte();
					long end = reader.BaseStream.Position + stringLength;
					CoreStringValue = Engine.ReadCodedString(reader, stringLength + 1);
					reader.BaseStream.Position = end;
					break;

				case ScriptOperandType.StringVariable: // 0x81
					Type = ScriptOperandType.StringVariable;
					CoreIntegerValue = reader.ReadUInt16();
					break;

				default: // Anything else
					Type = ScriptOperandType.Opcode;
					CoreIntegerValue = (byte)value;
					break;
			}
		}

		/// <summary></summary>
		public ScriptOpcode GetOpcode() { if (!IsOpcode) throw new InvalidOperationException("This is not an opcode token."); return (ScriptOpcode)CoreIntegerValue; }

		internal void Link() {
			switch (Type) {
				case ScriptOperandType.Address:
					foreach (ScriptInstruction instruction in Script.Instructions) {
						if (instruction.Address == AsAddress) {
							Target = instruction;
							instruction.TargetedByTokensMutable.Add(this);
							if (!instruction.TargetedByInstructionsMutable.Contains(Instruction))
								instruction.TargetedByInstructionsMutable.Add(Instruction);
							break;
						}
					}
					return;

				default:
					break;
			}
		}

		internal bool MatchTypeCode(ScriptArgument value) {
			switch (value) {
				case ScriptArgument.Address: return Type == ScriptOperandType.Address;
				case ScriptArgument.Comparison: return Type == ScriptOperandType.Comparison;
				case ScriptArgument.Literal:
				case ScriptArgument.Value: return Type == ScriptOperandType.Value;
				case ScriptArgument.ValueOrVariable: return Type == ScriptOperandType.Value || Type == ScriptOperandType.Variable || Type == ScriptOperandType.UnknownVariable;
				case ScriptArgument.String: return Type == ScriptOperandType.String || Type == ScriptOperandType.StringVariable;
				case ScriptArgument.Variable: return Type == ScriptOperandType.Variable || Type == ScriptOperandType.UnknownVariable;

				case ScriptArgument.Opcode:
				case ScriptArgument.Optional: throw new InvalidOperationException();
				default: throw new NotImplementedException();
			}
		}

		/// <summary></summary>
		public void ToRichText(RichTextBuilder builder) {
			Color foregroundColor = builder.ForegroundColor;

			try {
				if (!IsValid)
					builder.ForegroundColor = Color.Red;

				switch (Type) {
					default:
						builder.Append(this);
						break;
				}
			} finally {
				builder.ForegroundColor = foregroundColor;
			}
		}

		/// <summary></summary>
		public override string ToString() {
			string validString = IsValid ? "" : "!!INVALID!!";
			string text = "";

			switch (Type) {
				case ScriptOperandType.Value:
					text = string.Format("{0}", CoreIntegerValue);
					if (CoreIntegerValue > 9)
						text += string.Format("/{0:X2}h", CoreIntegerValue);
					if (CoreIntegerValue > 127)
						text += string.Format("/{0}", (sbyte)CoreIntegerValue);
					return text;

				case ScriptOperandType.Address:
					return string.Format("[{0:X}h]", AsAddress);

				case ScriptOperandType.String:
					return "\"" + CoreStringValue + "\"" + validString;

				case ScriptOperandType.StringVariable:
					return string.Format("String({0:X}h)", CoreIntegerValue);

				case ScriptOperandType.Opcode:
					if (typeof(ScriptOpcode).IsEnumDefined((ScriptOpcode)CoreIntegerValue))
						return ((ScriptOpcode)CoreIntegerValue).ToString() + validString;
					return string.Format("!!!UnknownOpcode!!!:{0:X2}{1}", CoreIntegerValue, validString);

				case ScriptOperandType.Variable:
					return string.Format("({0:X}h)", CoreIntegerValue);

				case ScriptOperandType.UnknownVariable:
					return string.Format("UnknownVariable({0:X}h)", CoreIntegerValue);

				case ScriptOperandType.Comparison:
					switch (AsComparison) {
						case ScriptComparison.Equal: return "==";
						case ScriptComparison.NotEqual: return "!=";
						default: return string.Format("Comparison({0:X2}h)!!!!!!!!", CoreIntegerValue);
					}

				case ScriptOperandType.None:
					return "!!!None!!!";

				default:
					throw new NotImplementedException();
			}
		}
	}
}
