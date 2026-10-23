
VAR accused_damage = false
VAR accused_stain = false
VAR accused_accessories = false
VAR accused_receipt = false
VAR asked_origin = false
VAR asked_character = false

Taylor: Hello! I'd like to return this dress, please. It's in perfect condition. You could eat off it. Please don't, though.
-> questions

=== questions ===
+ [Why are you returning this outfit?]
    Taylor: Oh, it just wasn't quite right for me. Beautiful craftsmanship, though. Whoever made it deserves a raise. And perhaps some recognition.
    -> questions

+ [When did you purchase this costume?]
    Taylor: Oh, a while ago. Time flies when you're... shopping. I don't remember the exact date.
    -> questions

+ [Have you ever worn this costume?]
    Taylor: No! Well, I tried it on. For quality assurance, obviously.
    -> questions

+ {not asked_origin} [Where did you buy this dress?]
    ~ asked_origin = true
    Taylor: Here! Unless you have another location? Perhaps one that sells considerably more elegant clothing?
    -> questions

+ {not asked_character} [What character is this costume supposed to be?]
    ~ asked_character = true
    Taylor: A princess? A wealthy widow? A woman with excellent taste? Does everything have to be a character?
    -> confession

+ [Accuse of anomaly]
    -> accusations

+ [Finish questioning]
    -> END

=== accusations ===
+ {not accused_damage} [Accuse of slight damage]
    ~ accused_damage = true
    Taylor: Damage?! Where?! I spent fourteen hours on those seams! I mean... whoever made it clearly cared about the stitching.
    -> accusations

+ {not accused_stain} [Accuse of a stain]
    ~ accused_stain = true
    Taylor: A stain? On THAT fabric? I'd sooner spill soup on the Mona Lisa. Not that I'm particularly attached to this dress.
    -> accusations

+ {not accused_accessories} [Accuse of missing accessories]
    ~ accused_accessories = true
    Taylor: Accessories? It's a dress, not a Christmas tree. The design is supposed to speak for itself. Apparently, nobody's listening.
    -> accusations

+ {not accused_receipt} [Accuse of a missing receipt]
    ~ accused_receipt = true
    Taylor: Right. The receipt. Funny story about that. Actually, it's not funny at all.
    -> confession

+ [Back to questions]
    -> questions

=== confession ===
Taylor: Alright, fine! I made it myself. Every stitch. Three shops turned me away, and one asked if I could make curtains instead. I know I shouldn't have lied. I just wanted someone to think my work was worth something.
-> questions

=== refund_response ===
Taylor: Wait, you're going to SELL it?! Oh my God, I need to call my mother. She thinks I'm still making curtains. Thank you! I promise my next collection will be considerably less fraudulent.
-> END

=== no_refund_response ===
Taylor: Yeah, that's fair. Sorry for lying. I guess I'll keep trying. Someone out there must need a very expensive dress. Or curtains, apparently.
-> END
