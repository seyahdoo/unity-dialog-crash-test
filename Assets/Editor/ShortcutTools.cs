using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Mobge
{
	public class ShortcutTools
	{
		public class Tool
		{
			public ActivationRule activation;
			public Func<bool> onPress;
			public Action onDrag;
			public Action onRelease;
			public Action onUpdate;
			public string name;

			public Tool(String name)
			{
				this.name = name;
			}

			public override string ToString()
			{
				return name;
			}
		}
		public class ActivationRule
		{
			public EventModifiers modifiers = EventModifiers.None;
			public int mouseButton = -1;
			public KeyCode key = KeyCode.None;
			public bool Satisfies(Event @event)
			{
				//Debug.Log(@event.button + " " + @event.modifiers + " " + ((modifiers & @event.modifiers) != modifiers));

				// Mask event modifiers with ignore modifiers
				// Resulting flags is checked against tool's activitation modifier flags
				if ((modifiers) != (@event.modifiers & ~EventModifiers.CapsLock & ~EventModifiers.FunctionKey))
					return false;

				if (mouseButton >= 0) {
					if (@event.button == mouseButton) {
						return true;
					}
				}
				if (key != KeyCode.None) {
					if (@event.keyCode == key) {
						return true;
					}
				}

				return false;
			}
		}
		public Tool ActiveTool {
			get { return _activeTool; }
		}

		///// <summary>
		///// Gets or sets the ignore modifiers.
		///// </summary>
		///// <value>The ignore modifiers.</value>
		///// <remarks>Ignore modifiers is utilized as a bitmask. Every bit is true, except the ignore flags.</remarks>
		//public EventModifiers IgnoreModifiers {
		//	get => _ignoreModifiers;
		//	set => _ignoreModifiers = ~EventModifiers.None ^ value;
		//}
		private List<Tool> tools = new List<Tool>();
		private List<Tool> keyTools = new List<Tool>();
		public Vector2 MouseStart {
			get; private set;
		}
		public Vector2 Mouse {
			get; private set;
		}
		public Ray MouseRay {
			get { return HandleUtility.GUIPointToWorldRay(Mouse); }
		}
		public void AddTool(Tool t)
		{
			if (t.activation.mouseButton >= 0) {
				tools.Add(t);
			} else if (t.activation.key != KeyCode.None) {
				keyTools.Add(t);
			} else {
				throw new Exception("mousebutton or key of activation of a tool must be set");
			}
		}

		private void GrabEvent(Tool tool, Event e, int offset)
		{
			e.Use();
			GUIUtility.hotControl = 437 + GetHashCode() + offset;
		}
		private void ReleaseEvent(Event e)
		{
			GUIUtility.hotControl = 0;
		}

		public void HandleInput()
		{
			Event @event = Event.current;
			if (@event.isMouse) {
				Mouse = @event.mousePosition;
				HandleEvent(@event, EventType.MouseDown, EventType.MouseDrag, EventType.MouseUp, tools);
			} else if (@event.isKey) {
				HandleEvent(@event, EventType.KeyDown, (EventType)(-1), EventType.KeyUp, keyTools);
			} else {
				if (_activeTool != null) {
					if (_activeTool.onUpdate != null) {
						_activeTool.onUpdate();
					}
				}
			}
			ShortcutsWindow.Push(tools, keyTools);
		}

		private void HandleEvent(Event @event, EventType down, EventType drag, EventType up, List<Tool> tools)
		{
			if (_activeTool == null) {
				if (@event.type == down) {
					for (int i = 0; i < tools.Count; i++) {
						Tool tool = tools[i];
						if (tool.activation.Satisfies(@event)) {
							//Debug.Log("asd: " + tool);
							if (tool.onPress == null || tool.onPress()) {
								_activeTool = tool;
								GrabEvent(tool, @event, i);
								MouseStart = @event.mousePosition;
								break;
							}
						}
					}
				}
			} else {
				// Mouse leaving the window triggers release because else after mouse has left the current window;
				// up condition does not exist therefore is never true; release does not get called
				if (@event.type == up || @event.type == EventType.MouseLeaveWindow) {
					ReleaseEvent(@event);
					if (_activeTool.onRelease != null) {
						_activeTool.onRelease();
					}
					_activeTool = null;
				} else if (@event.type == drag) {
					if (_activeTool.onDrag != null) {
						_activeTool.onDrag();
					}
				}
			}
		}

		private Tool _activeTool;
		private EventModifiers _ignoreModifiers = EventModifiers.None;

		public class ShortcutsWindow : EditorWindow
		{
			private static List<string> _usages = new List<string>();
			private static List<string> _names = new List<string>();
			private const string _namePrefix = "\u2022 ";
			private const string _usagePrefix = "\u21e8 ";
			private const string _toolStartCodon = "@@START-GO@@";
			private const string _toolEndCodon = "@@END-STOP@@";
			[MenuItem("Mobge/Editor Tool Shortcuts")]
			private static void Init()
			{
				// use reflection to get inspector window; to dock next to it.
				Type inspectorWindow = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
				ShortcutsWindow window = GetWindow<ShortcutsWindow>("Editor Tools: Shorcuts", true, inspectorWindow);
				window.Show();
			}
			private void Clear()
			{
				_usages.Clear();
				_names.Clear();
			}

			private static void EndRegisterToolset()
			{
				_usages.Add(_toolEndCodon);
				_names.Add(_toolEndCodon);
			}

			private static void BeginRegisterToolset()
			{
				_usages.Add(_toolStartCodon);
				_names.Add(_toolStartCodon);
			}

			private static void RegisterKeyCombo(string name, string usage)
			{
				var n = _namePrefix + name;
				var u = _usagePrefix + usage;
				_usages.Add(u);
				_names.Add(n);
			}

			protected void OnGUI()
			{
				if (GUILayout.Button("clear known shortcuts")) {
					Clear();
				}
				for (int i = 0; i < _usages.Count; i++) {
					if (_usages[i] == _toolStartCodon && _names[i] == _toolStartCodon) {
						EditorGUILayout.BeginVertical("Box");
						continue;
					}
					if (_usages[i] == _toolEndCodon && _names[i] == _toolEndCodon) {
						EditorGUILayout.EndVertical();
						continue;
					}
					EditorGUILayout.LabelField(_names[i], _usages[i]);
				}
			}

			internal static void Push(List<Tool> tools, List<Tool> keyTools)
			{
				// Basic guard statements
				if (tools != null && tools.Count != 0 && _names.Contains(_namePrefix + tools[0].name)) return;
				if (keyTools != null && keyTools.Count != 0 && _names.Contains(_namePrefix + keyTools[0].name)) return;

				string usage;
				BeginRegisterToolset();

				// parse mouse tools
				for (int i = 0; i < tools.Count; i++) {
					usage = string.Empty;
					if (tools[i].activation.modifiers != EventModifiers.None)
						usage += tools[i].activation.modifiers + " + ";                        // keyboard modifier name
					if (tools[i].activation.mouseButton >= 0)                                       // mouse button name
						switch (tools[i].activation.mouseButton) {
							case 0:
								usage += "Mouse Left";
								break;
							case 1:
								usage += "Mouse Right";
								break;
							case 2:
								usage += "Mouse Middle";
								break;
						}
					RegisterKeyCombo(tools[i].name, usage);
				}

				// parse keyboard tools
				for (int i = 0; i < keyTools.Count; i++) {
					usage = string.Empty;
					if (keyTools[i].activation.modifiers != EventModifiers.None)
						usage += keyTools[i].activation.modifiers + " + ";                          // keyboard modifier
					if (keyTools[i].activation.key != KeyCode.None)
						usage += keyTools[i].activation.key;                                     // keyboard button name
					RegisterKeyCombo(keyTools[i].name, usage);
				}
				EndRegisterToolset();
			}
		}
	}
}