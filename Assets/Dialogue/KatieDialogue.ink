
VAR accused_damage = false
VAR accused_stain = false
VAR accused_accessories = false
VAR accused_receipt = false

Katie: Hi. Yeah. I need a refund for this cat costume. And honestly, I should probably get an apology too.
-> questions

=== questions ===
+ [Why are you returning this outfit?]
    Katie: Because it's literally falling apart? I paid two hundred dollars to look like a cat, not something a cat dragged in. And before you ask, no, I didn't do anything to it. Your clothes are just, like, aggressively low quality.
    -> questions

+ [When did you purchase this costume?]
    Katie: Friday, I think? Before Halloween. Why, are you writing my biography? I remember because the girl at the register said I'd look cute. Which, okay, obviously. But still.
    -> questions

+ [Have you ever worn this costume?]
    Katie: Ew, no. I tried it on and immediately decided it was hideous. Like, imagine actually showing up to a party in this thing. Especially with Jessica there. God, she'd never shut up about it.
    -> questions

+ [Accuse of anomaly]
    -> accusations

+ [Finish questioning]
    -> END

=== accusations ===
+ {not accused_damage} [Accuse of slight damage]
    ~ accused_damage = true
    Katie: Oh my GOD, it's one little rip. You're acting like I put it through a wood chipper. Besides, that could've happened anywhere. In your store. In the bag. When some idiot stepped on my tail at the— Whatever. It's defective.
    -> accusations

+ {not accused_stain} [Accuse of a stain]
    ~ accused_stain = true
    Katie: What stain? That's probably just the lighting. Your store looks like a basement where people get murdered. Maybe invest in better bulbs before accusing your customers of crimes.
    -> accusations

+ {not accused_accessories} [Accuse of missing accessories]
    ~ accused_accessories = true
    Katie: The ears? Seriously? They're probably in the bag somewhere. Or under Jessica's couch. Which would be weird, because I obviously wasn't at her party. Anyway, why are you so obsessed with my ears? Freak.
    -> accusations

+ {not accused_receipt} [Accuse of a missing receipt]
    ~ accused_receipt = true
    Katie: I don't have it ON me. What, do you carry every piece of paper you've ever touched? I can probably find it in my dorm. Unless my roommate threw it out. She's literally useless.
    -> accusations

+ [Back to questions]
    -> questions

=== refund_response ===
Katie: Finally. Someone here has a functioning brain. Anyway, Jessica's throwing another party tonight, so I need to find something that won't fall apart before midnight.
-> END

=== no_refund_response ===
Katie: Are you SERIOUS? Do you know how much my parents spend at places like this? Whatever. I'm telling my entire sorority. Enjoy going out of business.
-> END
