using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public static class GeneralUtilities
    {

        public static Item[] VisibleItems(MOB mob, IPosition fromLocation)
        {
            Room startingRoom = fromLocation.forRoom;
            //Massively TODO.
            //In the long term, I think MOBs should have some short term memory about this, knowing who went where,
            //which might also be used to cache what they can see.
            //No great ideas for the calculations themselves right now. I probably want to keep this very simple somehow instead
            //of doing lots of trig and angles and sizes for everything.

            //Related idea TODO eventually:
            //Have a setting for 'Item shortcuts'. Add something like {a} then {b} then {c} and so on (going to {aaa} and so on if needed) to items a user observes.
            //Let user refer to items/things by {a} and so on instead of the name. Require that shortcuts not be reused for x amount of time.
            //Want to maybe do something extra fancy with this, for similar things user could conceivably confuse together or things that change unnoticed;
            //"Mook in red shirt {a} leaves east. Mook in red shirt {b} enters from the west." <-- gives away two different mooks
            //"Mook in red shirt {a} leaves east. Mook in red shirt {a} enters from the west." <-- implies same mook but maybe they are actually different but unusually similar.
            //"Mook in red shirt {a} leaves east. Damsel in distress {a} enters from the east." <-- similarly gives away that mook is disguised as damsel.

            //Another Related idea TODO eventually:
            //Allow user to ASSIGN shortcuts to items. This needs to be fundamentally different then the other item shortcuts though, maybe with a user-selected option for each item.
            // - Can remember only the item, assume it's the same item if see something similar elsewhere.
            // - Remember the item and position, assume it's the same item if see something similar in the same place later.
            // - Track the item, similar as above? probably not make this an option.

            return startingRoom.GetItems();
            
        }
    }
}
