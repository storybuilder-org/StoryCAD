using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCAD.Models;

public class RelationType : ObservableObject
{
    public string MemberRole;
    public string PartnerRole;

    public RelationType(string memberRole, string partnerRole)
    {
        MemberRole = memberRole;
        PartnerRole = partnerRole;
    }

    public override string ToString() { return MemberRole+ " => " + PartnerRole; }
}