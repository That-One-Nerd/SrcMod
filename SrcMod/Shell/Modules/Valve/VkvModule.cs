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
        catch
        {
#if DEBUG
            throw;
#else
            throw new($"Error parsing file to Valve KeyValues format: {e.Message}");
#endif
        }

        VkvModifyWhole(ref parentNode, ref parentNodeName);
    }

    #region The VKV Modification System
    private static void VkvModifyWhole(ref VkvNode parentNode, ref string parentNodeName)
    {
        // Generate reference context for passing to the modification methods.
        VkvModifyContext context = new()
        {
            displayLines = VkvModifyGetLines(parentNode, parentNodeName, 0)
        };

        // Make an initial printing of the vkv node.
        VkvModifyPrintAll(ref context, false);

        // Start modifying the parent node.
        VkvModifyNode(ref parentNode, ref parentNodeName, ref context, true);

        // Done editing, let's reset the cursor position and exit the command.
        Console.ResetColor();
        Console.SetCursorPosition(0, context.startingCursor + context.displayLines.Count);
    }

    private static VkvModifyOption VkvModifyNode(ref VkvNode node, ref string nodeName,
        ref VkvModifyContext context, bool isGlobal)
    {
        string add = $" {nodeName}";
        Console.Title += add;

        VkvModifyMode mode = VkvModifyMode.Default;

        int subIndex = -1; // Represents the index of the sub node currently being modified.
        // If the variable is set to -1, it represents the title.

        VkvSingleNode? single = node as VkvSingleNode;
        VkvTreeNode? tree = node as VkvTreeNode;

        VkvModifyOption? option = null;
        while (true)
        {
            // Color the display white, wait for a key, then reset and handle the key press.
            Console.SetCursorPosition(0, context.startingCursor + context.lineIndex);
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.Write(context.displayLines[context.lineIndex]);
            Console.ResetColor();

            if (!option.HasValue) option = Console.ReadKey(true).Key switch
            {
                ConsoleKey.DownArrow => VkvModifyOption.IncSubIndex,
                ConsoleKey.UpArrow => VkvModifyOption.DecSubIndex,
                _ => VkvModifyOption.Nothing
            };

            Console.CursorLeft = 0; // This is assuming the cursor hasn't moved, which it shouldn't.
            Console.Write(context.displayLines[context.lineIndex]);

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
                                    VkvModifyNode(ref subNodeValue, ref subNodeKey, ref context, false);

                                // Update the parent node with our modified sub node.
                                tree[subIndex] = new(subNodeKey, subNodeValue);

                                // Set the next instruction.
                                option = status;
                            }
                        }
                        else if (subIndex == tree.SubNodeCount + 1)
                        {
                            // We're outside the maximum sub nodes. Let's increment the parent
                            // sub index and end this method.

                            Console.Title = Console.Title[..^add.Length];
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
                                        VkvModifyNode(ref subNodeValue, ref subNodeKey, ref context, false);

                                    // Update the parent node with our modified sub node.
                                    tree[subIndex] = new(subNodeKey, subNodeValue);

                                    // Set the next instruction.
                                    option = status;
                                }
                            }
                            else
                            {
                                // TODO: This is where we can decide to add sub nodes.
                            }
                        }
                    }
                    else
                    {
                        // We aren't in a tree. We just change the parent sub index and
                        // end this method (and increment the line).

                        Console.Title = Console.Title[..^add.Length];
                        return VkvModifyOption.IncSubIndex;
                    }
                    break;

                case VkvModifyOption.DecSubIndex:
                    if (tree is not null)
                    {
                        subIndex--;

                        if (subIndex == -2)
                        {
                            // We're outside the maximum sub nodes. Let's decrement the parent
                            // sub index and end this method (and decrement the line).

                            Console.Title = Console.Title[..^add.Length];
                            return VkvModifyOption.DecSubIndex;
                        }
                        else if (subIndex == -1)
                        {
                            // We just shifted down from the title to the first sub node.
                            // We need to overlook the next line, '{'.

                            context.lineIndex -= 2;
                        }
                        else
                        {
                            // We're in a valid range. Let's just change the sub node we're
                            // focused on.
                            context.lineIndex--;
                        }
                    }
                    else
                    {
                        // We aren't in a tree. We just change the parent sub index and
                        // end this method (and decrement the line).

                        Console.Title = Console.Title[..^add.Length];
                        return VkvModifyOption.DecSubIndex;
                    }
                    break;
            }
        }
    }

    private static void VkvModifyPrintAll(ref VkvModifyContext context, bool resetCursor)
    {
        Int2 cursorPos = (Console.CursorLeft, Console.CursorTop);

        Console.SetCursorPosition(0, context.startingCursor);
        foreach (string line in context.displayLines)
        {
            Console.WriteLine(line);
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

    private class VkvModifyContext
    {
        public required List<string> displayLines;
        public int lineIndex;
        public readonly int startingCursor;

        public VkvModifyContext()
        {
            lineIndex = 0;
            startingCursor = Console.CursorTop;
        }
    }

    private enum VkvModifyMode
    {
        Default
    }
    private enum VkvModifyOption
    {
        Nothing,
        IncSubIndex,
        DecSubIndex
    }
    #endregion
}
