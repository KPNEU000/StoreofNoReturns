
VAR accused_damage = false
VAR accused_stain = false
VAR accused_accessories = false
VAR accused_receipt = false

Frank: Yeah, hi. I'd like to return this zombie costume. Apparently, a hundred dollars gets you two shirts' worth of fabric and a personal insult.
-> questions

=== questions ===
+ [Why are you returning this outfit?]
    Frank: Look at it. It's ripped to shreds. I ordered a zombie costume, not the aftermath of a lawnmower accident. You people really charge a premium for holes, huh?
    -> questions

+ [When did you purchase this costume?]
    Frank: Right before Halloween. Ordered it online. Hundred bucks. For that price, I expected something a little more luxurious than roadkill chic.
    -> questions

+ [Have you ever worn this costume?]
    Frank: No. Never. Didn't even try it on. I spent Halloween at home, alone, minding my business. Which is apparently a crime now.
    -> questions

+ [Accuse of anomaly]
    -> accusations

+ [Finish questioning]
    -> END

=== accusations ===
+ {not accused_damage} [Accuse of slight damage]
    ~ accused_damage = true
    Frank: Oh, NOW the holes are a problem? Funny, because when I bought it, you called them "authentic undead detailing." What, did the zombie come back to life and void the warranty?
    -> accusations

+ {not accused_stain} [Accuse of a stain]
    ~ accused_stain = true
    Frank: That's fake blood. Obviously. What else would it be, grape soda? Haha. Who even drinks grape soda at a Halloween party? Not me. I wasn't at a party.
    -> accusations

+ {not accused_accessories} [Accuse of missing accessories]
    ~ accused_accessories = true
    Frank: What accessories? A shovel? A coffin? Was I supposed to bring my own grave? Because nobody mentioned that in the product description.
    -> accusations

+ {not accused_receipt} [Accuse of a missing receipt]
    ~ accused_receipt = true
    Frank: I ordered it online. You want me to print the internet? I've got the confirmation email right here. Do I need to fax you my entire browser history too?
    -> accusations

+ [Back to questions]
    -> questions

=== refund_response ===
Frank: Wait, seriously? A hundred bucks? Wow. You guys really are committed to the whole brainless thing. Anyway, thanks! Pleasure doing business with you.
-> END

=== no_refund_response ===
Frank: Unbelievable. You sell me clothes that look like they've been buried for six months, and somehow I'M the problem? Enjoy your one-star review. Zero stars if I can figure out how.
-> END
