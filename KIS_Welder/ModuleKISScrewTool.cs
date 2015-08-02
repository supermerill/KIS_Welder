using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KIS;

namespace KIS_Welder
{
	public class ModuleKISScrewTool : ModuleKISItemAttachTool
	{

		[KSPField]
		public bool isWeldingTool = false; // false => screw tool

		// check if the pointed part can be attach with our current tool
		public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom)
		{
			// Check if grab key is pressed
			//if (useFrom == KIS_Item.UseFrom.KeyDown)
			//{
			//    KISAddonPickup.instance.EnableAttachMode();
			//}

			// Check if grab key is pressed
			if (useFrom == KIS_Item.UseFrom.KeyDown)
			{
				if (KISAddonPointer.isRunning && KISAddonPointer.pointerTarget != KISAddonPointer.PointerTarget.PartMount)
				{
					//float attachPartMass = KISAddonPointer.partToAttach.mass + KISAddonPointer.partToAttach.GetResourceMass();
					//if (attachPartMass < attachMaxMass)
					{
						//test if the tool can attach this part (screw or weld)
						//default (when ModuleAttachMode is not here) : magical yes
						bool testIfCanAttachPart = true;
						if (KISAddonPointer.partToAttach.Modules.Contains("ModuleAttachMode"))
						{
							ModuleAttachMode mkpam = (KISAddonPointer.partToAttach.Modules["ModuleAttachMode"] as ModuleAttachMode);
							if (!mkpam.canBeWeld && !mkpam.canBeScrewed)
							{
								ScreenMessages.PostScreenMessage("This part can't be attached", 5, ScreenMessageStyle.UPPER_CENTER);
								testIfCanAttachPart = false;
							}
							else
							{
								testIfCanAttachPart = isWeldingTool ? mkpam.canBeWeld : mkpam.canBeScrewed;
								item.PlaySound(KIS_Shared.bipWrongSndPath);
								if (!testIfCanAttachPart)
								{
									ScreenMessages.PostScreenMessage("This part can't be attached with this tool: it need a " +
										(isWeldingTool ? "screwdriver" : "weld tool"), 5, ScreenMessageStyle.UPPER_CENTER);
								}
							}
						}
						if (testIfCanAttachPart)
						{
							//KISAddonPickup.instance.pointerMode = KISAddonPickup.PointerMode.Attach;
							//KISAddonPointer.allowStack = allowStack;
							//item.PlaySound(changeModeSndPath);
							KISAddonPickup.instance.EnableAttachMode();
						}
					}
					//else
					//{
					//	item.PlaySound(KIS_Shared.bipWrongSndPath);
					//	ScreenMessages.PostScreenMessage("This part is too heavy for this tool", 5, ScreenMessageStyle.UPPER_CENTER);
					//}
				}

				if (useFrom == KIS_Item.UseFrom.KeyUp)
				{
					KISAddonPickup.instance.DisableAttachMode();
				}

			}
			//if (useFrom == KIS_Item.UseFrom.KeyUp)
			//{
			//    if (KISAddonPointer.isRunning && KISAddonPickup.instance.pointerMode == KISAddonPickup.PointerMode.Attach)
			//    {
			//        KISAddonPickup.instance.pointerMode = KISAddonPickup.PointerMode.Drop;
			//        KISAddonPointer.allowStack = false;
			//        item.PlaySound(changeModeSndPath);
			//    }
			//}

		}

		// to detach something it need to be screwed and we need a screwing tool
		public override bool OnCheckDetach(Part partToDetach, ref string[] errorMsg)
		{
			if (!base.OnCheckDetach(partToDetach, ref errorMsg))
			{
				return false;
			}
			//Debug.Log("OnCheckDetach2 " + (partToDetach == null ? "null" : partToDetach.name)
			//    + " =parent=> " + (partToDetach.parent == null ? "null" : partToDetach.parent.name)
			//    + " & has mod? " + (partToDetach.Modules != null));
			if (partToDetach.Modules == null) return true;
			// Check if part can be detached from parent with this tool
			//Debug.Log("OnCheckDetach2.2 " + partToDetach.Modules.Contains("ModuleAttachMode"));
			if (partToDetach.parent && partToDetach.Modules.Contains("ModuleAttachMode"))
			{
				ModuleAttachMode mkpam = (partToDetach.Modules["ModuleAttachMode"] as ModuleAttachMode);
				//Debug.Log("OnCheckDetach2.3 " + isWeldingTool + ", " + mkpam.canBeScrewed + ", " + mkpam.canBeWeld + " : " + mkpam.isWelded);
				if (!mkpam.canBeWeld && !mkpam.canBeScrewed)
				{
					errorMsg = new string[] { "KIS/Textures/forbidden", "Can't grab", "(Part can't be detached without a tool" };
					return false;
				}
				if (isWeldingTool && (!mkpam.canBeWeld || !mkpam.isWelded))
				{
					errorMsg = new string[] { "KIS/Textures/forbidden", "Can't grab", "(Part can't be detached without a screwdriver" };
					return false;
				}
				if (mkpam.isWelded || !mkpam.canBeScrewed)
				{
					errorMsg = new string[] { "KIS/Textures/forbidden", "Can't grab", "(Part can't be detached : it's welded" };
					return false;
				}
			}
			//if can't find the module => don't check, open bar!
			return true;
		}

		// apply the screw/weld property
		public override void OnAttachToolUsed(Part srcPart, Part tgtPart, KISAttachType moveType, KISAddonPointer.PointerTarget pointerTarget)
		{
			base.OnAttachToolUsed(srcPart, tgtPart, moveType, pointerTarget);
			//Debug.Log("OnItemMove2 begin" + (srcPart == null ? "null" : srcPart.name) + " => " +
			//    (tgtPart == null ? "null" : tgtPart.name) + ", " + moveType + ", " + pointerTarget);
			//set welded if needed
			if ((moveType == KISAttachType.ATTACH)
				&& srcPart.Modules.Contains("ModuleAttachMode")
				)
			{
				ModuleAttachMode mkpam = srcPart.Modules["ModuleAttachMode"] as ModuleAttachMode;
				mkpam.isWelded = isWeldingTool;
				//Debug.Log("OnItemMove2 " + isWeldingTool);
			}
		}

		//to attach, the receiver (parent) parts need the same canWeld proty than the tool (children part already check in OnItemUse)
		protected override bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg)
		{
			//Debug.Log("OnCheckAttach2 begin ");
			if (!base.OnCheckAttach(srcPart, tgtPart, ref toolInvalidMsg)) return false;
			//default (when ModuleAttachMode is not here) : magical yes
			bool cannotAttach = false;
			bool wrongTool = false;
			if (srcPart.Modules.Contains("ModuleAttachMode"))
			{
				ModuleAttachMode mkpam = (srcPart.Modules["ModuleAttachMode"] as ModuleAttachMode);
				cannotAttach = !mkpam.canBeWeld && !mkpam.canBeScrewed;
				if (!cannotAttach) wrongTool = isWeldingTool ? !mkpam.canBeWeld : !mkpam.canBeScrewed;
			}
			//Debug.Log("OnCheckAttach2 " + (srcPart == null ? "null" : srcPart.name) + " => " +
			//    (tgtPart == null ? "null" : tgtPart.name) + ", " + wrongTool + ", " + cannotAttach);
			if (!wrongTool && !cannotAttach)
			{
				return true;
			}
			else
			{
				if (cannotAttach) toolInvalidMsg = ("Target part do not allow attach !");
				else toolInvalidMsg = "Target part need a "
						+ (isWeldingTool ? "screwdriver" : "welding tool") + " to attach something on it !";
				return false;
			}
		}

	}
}
