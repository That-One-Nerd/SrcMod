namespace SrcMod.Shell.Modules.Valve;

[Module("vkv")]
public static class VkvModule
{
    [Command("create")]
    [CanCancel(false)]
    public static void CreateVkv(string path)
    {
        if (File.Exists(path)) throw new($"File already exists at \"{path}\". Did you mean to run \"vkv edit\"?");

        VkvNode parentNode = new VkvTreeNode()
        {
            { "key", new VkvSingleNode("value") }
        };
        string parentNodeName = "tree";

        VkvModifyWhole(ref parentNode, ref parentNodeName);

        try
        {
            FileStream fs = new(path, FileMode.Create);
            SerializeVkv.Serialize(fs, parentNode, parentNodeName);
        }
        catch (Exception ex)
        {
#if DEBUG
            throw;
#else
            throw new($"Error serializing result to file: {ex.Message}");
#endif
        }
    }

    [Command("edit")]
    [CanCancel(false)]
    public static void EditVkv(string path)
    {
        if (!File.Exists(path)) throw new($"No file exists at \"{path}\". Did you mean to run \"vkv create\"?");

        VkvNode? parentNode;
        string parentNodeName = string.Empty;
        try
        {
            FileStream fs = new(path, FileMode.Open);
            parentNode = SerializeVkv.Deserialize(fs, out parentNodeName);

            if (parentNode is null) throw new("Deserialized VKV node is null.");
        }
        catch (Exception ex)
        {
#if DEBUG
            throw;
#else
            throw new($"Error parsing file to Valve KeyValues format: {ex.Message}");
#endif
        }

        VkvModifyWhole(ref parentNode, ref parentNodeName);

        try
        {
            FileStream fs = new(path, FileMode.Create);
            SerializeVkv.Serialize(fs, parentNode, parentNodeName);
        }
        catch (Exception ex)
        {
#if DEBUG
            throw;
#else
            throw new($"Error serializing result to file: {ex.Message}");
#endif
        }
    }

#region The VKV Modification System
    private static void VkvModifyWhole(ref VkvNode rootNode, ref string rootNodeName)
    {
        // Generate reference context for passing to the modification methods.
        VkvModifyContext context = new()
        {
            displayLines = VkvModifyGetLines(rootNode, rootNodeName, 0),
            rootNode = rootNode,
            rootNodeName = rootNodeName
        };

        // Make an initial printing of the vkv node.
        VkvModifyPrintAll(ref context, false);

        // Start modifying the root node.
        VkvTreeNode? nullNode = null;
        int nullInt = default;
        VkvModifyNode(ref rootNode, ref rootNodeName, ref context, true, ref nullNode, ref nullInt);

        // Done editing, let's reset the cursor position and exit the command.
        Console.ResetColor();
        Console.SetCursorPosition(0, context.startingCursor + context.displayLines.Count);
    }

    private static VkvModifyOption VkvModifyNode(ref VkvNode node, ref string nodeName,
        ref VkvModifyContext context, bool isGlobal, ref VkvTreeNode? parentNode, ref int parentSubIndex)
    {
#if DEBUG
        string add = $" {nodeName}";
        Console.Title += add;
#endif

        int subIndex = -1; // Represents the index of the sub node currently being modified.
        // If the variable is set to -1, it represents the title.

        VkvSingleNode? single = node as VkvSingleNode;
        VkvTreeNode? tree = node as VkvTreeNode;

        VkvModifyOption? option = null;
        VkvModifySelection selection = VkvModifySelection.Name;
        while (true)
        {
            // Color the display white, wait for a key, then reset and handle the key press.
            string line = context.displayLines[context.lineIndex];

            Console.SetCursorPosition(0, context.startingCursor + context.lineIndex);
            Console.Write(Whitify(line, selection));
            Console.ResetColor();

            if (!option.HasValue) option = Console.ReadKey(true).Key switch
            {
                ConsoleKey.DownArrow => VkvModifyOption.IncSubIndex,
                ConsoleKey.UpArrow => VkvModifyOption.DecSubIndex,
                ConsoleKey.RightArrow => VkvModifyOption.RShiftMode,
                ConsoleKey.LeftArrow => VkvModifyOption.LShiftMode,
                ConsoleKey.Enter => VkvModifyOption.Use,
                ConsoleKey.Escape => VkvModifyOption.ExitAll,
                _ => VkvModifyOption.Nothing
            };

            Console.CursorLeft = 0; // This is assuming the cursor hasn't moved, which it shouldn't.
            Console.Write(line + new string(' ', Console.WindowWidth - line.Length));

            // Now we handle the key press.
            switch (option)
            {
                case VkvModifyOption.IncSubIndex:
                    if (tree is not null)
                    {
                        subIndex++;

                        if (subIndex == 0)
                        {
                            // We just shifted down from the title to the first sub node.
                            // We need to overlook the next line, '{'.

                            context.lineIndex += 2;

                            // Now we also need to start modification of the first sub node.
                            KeyValuePair<string, VkvNode>? subNode = tree[subIndex];
                            if (subNode is not null)
                            {
                                string subNodeKey = subNode.Value.Key;
                                VkvNode subNodeValue = subNode.Value.Value;
                                VkvModifyOption status = 
                                    VkvModifyNode(ref subNodeValue, ref subNodeKey, ref context, false, ref tree, ref subIndex);

                                // Update the parent node with our modified sub node.
                                tree![subIndex] = new(subNodeKey, subNodeValue);

                                // Set the next instruction.
                                option = status;
                            }
                        }
                        else if (subIndex == tree.SubNodeCount + 1)
                        {
                            // We're outside the maximum sub nodes. Let's increment the parent
                            // sub index and end this method.
                            // Incrementing the line index to overlook the next line, '}'

#if DEBUG
                            Console.Title = Console.Title[..^add.Length];
#endif
                            context.lineIndex++;
                            return VkvModifyOption.IncSubIndex;
                        }
                        else
                        {
                            // We're in a valid range. Let's just change the sub node we're
                            // focused on.
                            context.lineIndex++;

                            if (subIndex < tree.SubNodeCount)
                            {
                                // We are talking about an already existing node, so we also need
                                // to start modification of the first sub node.
                                KeyValuePair<string, VkvNode>? subNode = tree[subIndex];
                                if (subNode is not null)
                                {
                                    string subNodeKey = subNode.Value.Key;
                                    VkvNode subNodeValue = subNode.Value.Value;

                                    VkvModifyOption status = 
                                        VkvModifyNode(ref subNodeValue, ref subNodeKey, ref context, false, ref tree, ref subIndex);

                                    // Update the parent node with our modified sub node.
                                    tree![subIndex] = new(subNodeKey, subNodeValue);

                                    // Set the next instruction.
                                    option = status;
                                }
                            }
                            else
                            {
                                // TODO: This is where we can decide to add sub nodes.
                                option = null;
                                selection = VkvModifySelection.CreateNew;

#if DEBUG
                                string secondAdd = " [CREATE NEW]";
                                add += secondAdd;
                                Console.Title += secondAdd;
#endif
                            }
                        }
                    }
                    else
                    {
                        // We aren't in a tree. We just change the parent sub index and
                        // end this method (and increment the line).

#if DEBUG
                        Console.Title = Console.Title[..^add.Length];
#endif
                        return VkvModifyOption.IncSubIndex;
                    }
                    break;

                case VkvModifyOption.DecSubIndex:
                    // TODO: Implement when moving downward is complete.
                    // It's a little weird to not be able to move back up,
                    // I know, but it's gonna be weirder to implement, and
                    // I only want to do it once.
                    option = null;
                    break;

                case VkvModifyOption.RShiftMode:
                    selection = selection switch
                    {
                        VkvModifySelection.Delete => VkvModifySelection.Name,
                        VkvModifySelection.Name => single is null ? selection : VkvModifySelection.Value,
                        _ => selection
                    };
                    option = null;
                    break;

                case VkvModifyOption.LShiftMode:
                    selection = selection switch
                    {
                        VkvModifySelection.Name => isGlobal ? VkvModifySelection.Name : VkvModifySelection.Delete,
                        VkvModifySelection.Value => VkvModifySelection.Name,
                        _ => selection
                    };
                    option = null;
                    break;

                case VkvModifyOption.Use:
                    switch (selection)
                    {
                        case VkvModifySelection.Delete:
                            string unrefNodeName = nodeName;
                            VkvNode unrefNode = node;
                            parentNode![parentSubIndex - 1] = null;

                            // Inefficient, yeah, but again, this is intended to
                            // be used by humans, not robots. Feel free to improve
                            // it if you want, but I probably won't.

                            List<string> newLines = VkvModifyGetLines(context.rootNode, context.rootNodeName, 0);
                            int endBuffer = context.displayLines.Count - newLines.Count;
                            context.displayLines = newLines;

                            VkvModifyPrintAll(ref context, true, true);

                            // TODO: Kinda works, but it's quite buggy. Fix later.

                            Console.SetCursorPosition(0, context.startingCursor + context.displayLines.Count);
                            for (int i = 0; i < endBuffer; i++) Console.WriteLine(new string(' ', Console.WindowWidth));
                            break;

                        case VkvModifySelection.Name:
                            VkvModifyRefactorName(ref nodeName, ref context);
                            break;

                        case VkvModifySelection.Value:
                            if (single is null)
                            {
                                option = null;
                                continue;
                            }
                            VkvModifyRefactorValue(ref single, ref context);
                            break;

                        case VkvModifySelection.CreateNew:
                            // TODO
                            break;
                    }
                    option = null;
                    break;

                case VkvModifyOption.ExitAll: return VkvModifyOption.ExitAll;

                default:
                    option = null;
                    continue;
            }
        }
    }

    private static void VkvModifyRefactorName(ref string nodeName, ref VkvModifyContext context)
    {
        string originalName = nodeName;
        string edit = context.displayLines[context.lineIndex];

        int firstQuote = edit.IndexOf('\"'),
            secondQuote = edit[(firstQuote + 1)..].IndexOf('\"') + firstQuote + 1;

        int displayIndex = secondQuote, nameIndex = nodeName.Length;

        int additionalAnsiTakeoff = 0;

        {
            // I almost never do this ("this" being the brackets while
            // already inside a method), but I also don't like
            // keeping temporary variables around for no reason.
            bool tempActive = false;
            for (int i = 0; i < displayIndex; i++)
            {
                if (edit[i] == '\x1b') tempActive = true;
                if (tempActive) additionalAnsiTakeoff++;
                if (tempActive && edit[i] == 'm') tempActive = false;
            }
        }

        while (true)
        {
            Console.CursorLeft = 0;
            Console.Write(Whitify(edit, VkvModifySelection.Name, true));

            Console.CursorLeft = displayIndex - additionalAnsiTakeoff;
            Console.CursorVisible = true;
            ConsoleKeyInfo key = Console.ReadKey(true);
            Console.CursorVisible = false;
            if (char.IsControl(key.KeyChar))
            {
                // TODO: Adding clipboard support might be cool (but also a pain).
                bool end = false;
                switch (key.Key)
                {
                    case ConsoleKey.Backspace:
                        if (nameIndex > 0)
                        {
                            nodeName = nodeName.Remove(nameIndex - 1, 1);
                            edit = edit.Remove(displayIndex - 1, 1) + ' ';
                            displayIndex--;
                            nameIndex--;
                        }
                        break;

                    case ConsoleKey.Delete:
                        if (nameIndex < nodeName.Length)
                        {
                            nodeName = nodeName.Remove(nameIndex, 1);
                            edit = edit.Remove(displayIndex, 1) + ' ';
                        }
                        break;

                    case ConsoleKey.Escape:
                        nodeName = originalName;
                        edit = context.displayLines[context.lineIndex];
                        end = true;
                        break;

                    case ConsoleKey.Enter:
                        end = true;
                        break;

                    case ConsoleKey.LeftArrow:
                        if (nameIndex > 0)
                        {
                            displayIndex--;
                            nameIndex--;
                        }
                        break;

                    case ConsoleKey.RightArrow:
                        if (nameIndex < nodeName.Length)
                        {
                            displayIndex++;
                            nameIndex++;
                        }
                        break;
                }
                if (end) break;
            }
            else
            {
                nodeName = nodeName.Insert(nameIndex, key.KeyChar.ToString());
                edit = edit.Insert(displayIndex, key.KeyChar.ToString()).TrimEnd();
                displayIndex++;
                nameIndex++;
            }
        }

        context.displayLines[context.lineIndex] = edit.TrimEnd();
    }
    private static void VkvModifyRefactorValue(ref VkvSingleNode node, ref VkvModifyContext context)
    {
        string value = node.value?.ToString() ?? string.Empty,
               edit = context.displayLines[context.lineIndex];

        int firstQuote = edit.IndexOf('\"'),
            secondQuote = edit[(firstQuote + 1)..].IndexOf('\"') + firstQuote + 1,
            thirdQuote = edit[(secondQuote + 1)..].IndexOf('\"') + secondQuote + 1,
            fourthQuote = edit[(thirdQuote + 1)..].IndexOf('\"') + thirdQuote + 1;

        int displayIndex = fourthQuote, valueIndex = value.Length;

        int additionalAnsiTakeoff = 0;

        {
            // See my opinions of brackets while already
            // inside a method in the `VkvModifyRefactorName`
            // method.
            bool tempActive = false;
            for (int i = 0; i < displayIndex; i++)
            {
                if (edit[i] == '\x1b') tempActive = true;
                if (tempActive) additionalAnsiTakeoff++;
                if (tempActive && edit[i] == 'm') tempActive = false;
            }
        }

        while (true)
        {
            Console.CursorLeft = 0;
            Console.Write(Whitify(edit, VkvModifySelection.Value, true));

            Console.CursorLeft = displayIndex - additionalAnsiTakeoff;
            Console.CursorVisible = true;
            ConsoleKeyInfo key = Console.ReadKey(true);
            Console.CursorVisible = false;
            if (char.IsControl(key.KeyChar))
            {
                // TODO: Adding clipboard support might be cool (but also a pain).
                bool end = false;
                switch (key.Key)
                {
                    case ConsoleKey.Backspace:
                        if (valueIndex > 0)
                        {
                            value = value.Remove(valueIndex - 1, 1);
                            edit = edit.Remove(displayIndex - 1, 1) + ' ';
                            displayIndex--;
                            valueIndex--;
                        }
                        break;

                    case ConsoleKey.Delete:
                        if (valueIndex < value.Length)
                        {
                            value = value.Remove(valueIndex, 1);
                            edit = edit.Remove(displayIndex, 1) + ' ';
                        }
                        break;

                    case ConsoleKey.Escape:
                        edit = context.displayLines[context.lineIndex];
                        end = true;
                        break;

                    case ConsoleKey.Enter:
                        node.value = value;
                        end = true;
                        break;

                    case ConsoleKey.LeftArrow:
                        if (valueIndex > 0)
                        {
                            displayIndex--;
                            valueIndex--;
                        }
                        break;

                    case ConsoleKey.RightArrow:
                        if (valueIndex < value.Length)
                        {
                            displayIndex++;
                            valueIndex++;
                        }
                        break;
                }
                if (end) break;
            }
            else
            {
                value = value.Insert(valueIndex, key.KeyChar.ToString());
                edit = edit.Insert(displayIndex, key.KeyChar.ToString()).TrimEnd();
                displayIndex++;
                valueIndex++;
            }
        }

        context.displayLines[context.lineIndex] = edit.TrimEnd();
    }

    private static void VkvModifyPrintAll(ref VkvModifyContext context, bool resetCursor, bool flushLine = false)
    {
        Int2 cursorPos = (Console.CursorLeft, Console.CursorTop);

        Console.SetCursorPosition(0, context.startingCursor);
        foreach (string line in context.displayLines)
        {
            Console.Write(line);
            if (flushLine) Console.Write(new string(' ', Console.WindowWidth - line.Length));
            Console.WriteLine();
            Console.ResetColor();
        }

        if (resetCursor) Console.SetCursorPosition(cursorPos.x, cursorPos.y);
    }

    private static List<string> VkvModifyGetLines(VkvNode node, string nodeName, int indent)
    {
        int spaceCount = indent * 4,
            nextSpaceCount = (indent + 1) * 4;

        List<string> lines = new();

        if (node is VkvSingleNode single) lines.Add(new string(' ', spaceCount) + $"\x1b[33m\"{nodeName}\"" +
                                                    $"  \x1b[32m\"{single.value}\"");
        else if (node is VkvTreeNode tree)
        {
            lines.Add(new string(' ', spaceCount) + $"\x1b[94m\"{nodeName}\"");
            lines.Add(new string(' ', spaceCount) + "{");
            foreach (KeyValuePair<string, VkvNode> pair in tree)
            {
                lines.AddRange(VkvModifyGetLines(pair.Value, pair.Key, indent + 1));
            }
            lines.Add(new string(' ', nextSpaceCount) + "\x1b[35m...");
            lines.Add(new string(' ', spaceCount) + "}");
        }
        else lines.Add(new string(' ', spaceCount) + "\x1b[31mError");

        return lines;
    }

    private static string Whitify(string content, VkvModifySelection selection, bool blink = false)
    {
        StringBuilder result = new();

        // This is definitely optimizable, but I don't feel like doing that yet.
        // Maybe in the future.
        // For future reference, when (if) this is optimized, I am doing stuff like this in this
        // method along with the name and value refactoring methods.
        int firstQuote = content.IndexOf('\"'),
            secondQuote = content[(firstQuote + 1)..].IndexOf('\"') + firstQuote + 1,
            thirdQuote = content[(secondQuote + 1)..].IndexOf('\"') + secondQuote + 1,
            fourthQuote = content[(thirdQuote + 1)..].IndexOf('\"') + thirdQuote + 1;

        int startChar = 0;
        while (char.IsWhiteSpace(content[startChar])) startChar++;

        int endChar = content.Length - 1;
        while (char.IsWhiteSpace(content[endChar])) endChar--;

        switch (selection)
        {
            case VkvModifySelection.Name:
                if (firstQuote < 0 || secondQuote < 0) return content;

                result.Append(content[..firstQuote]);
                if (blink)
                {
                    result.Append("\"\x1b[5m");
                    result.Append(content[(firstQuote + 1)..secondQuote]);
                    result.Append("\x1b[25m\"");
                }
                else
                {
                    result.Append("\x1b[107m");
                    result.Append(content[firstQuote..(secondQuote + 1)]);
                    result.Append("\x1b[0m");
                }
                result.Append(content[(secondQuote + 1)..]);
                break;

            case VkvModifySelection.Value:
                if (thirdQuote < 0 || fourthQuote < 0) return content;

                result.Append(content[..thirdQuote]);
                if (blink)
                {
                    result.Append("\"\x1b[5m");
                    result.Append(content[(thirdQuote + 1)..fourthQuote]);
                    result.Append("\x1b[25m\"");
                }
                else
                {
                    result.Append("\x1b[107m");
                    result.Append(content[thirdQuote..(fourthQuote + 1)]);
                    result.Append("\x1b[0m");
                }
                result.Append(content[(fourthQuote + 1)..]);
                break;

            case VkvModifySelection.Delete:
                const string addDelete = "[Delete]";

                result.Append($"\x1b[107m\x1b[31m{addDelete}\x1b[0m ");
                if (addDelete.Length + 1 > startChar) result.Append(content[startChar..]);
                else result.Append(content[(addDelete.Length + 1)..]);
                break;

            case VkvModifySelection.CreateNew:
                result.Append(content[..startChar]);
                result.Append("\x1b[107m");
                result.Append(content[startChar..(endChar + 1)]);
                result.Append("\x1b[0m");
                result.Append(content[(endChar + 1)..]);
                break;
        }

        return result.ToString();
    }

    private class VkvModifyContext
    {
        public required List<string> displayLines;
        public int lineIndex;
        public required VkvNode rootNode;
        public required string rootNodeName;
        public readonly int startingCursor;

        public VkvModifyContext()
        {
            lineIndex = 0;
            startingCursor = Console.CursorTop;
        }
    }

    private enum VkvModifyOption
    {
        Nothing,
        IncSubIndex,
        DecSubIndex,
        RShiftMode,
        LShiftMode,
        Use,
        ExitAll
    }
    private enum VkvModifySelection
    {
        Delete,
        Name,
        Value,
        CreateNew
    }
#endregion
}
