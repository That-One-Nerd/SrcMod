using SrcMod.Shell.Valve.Vkv;

namespace SrcMod.Shell.Valve.Vkv.ObjectModels;

public interface IVkvConvertible
{
    public VkvNode ToNodeTree();
}