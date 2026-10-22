
VAR accused_damage = false
VAR accused_stain = false
VAR accused_accessories = false
VAR accused_receipt = false

Shelly: Oh, hello, sweetheart! Sorry to bother you so late. I'd like to return this bird costume, please. It's still unopened, tags and everything.
-> questions

=== questions ===
+ [Why are you returning this outfit?]
    Shelly: I adore birds! I even found a mechanical singing one at a little shop in Brooklyn. But when I asked to buy it, the gentleman treated me like Julia Roberts from Pretty Woman. After that, I just couldn't bring myself to wear this costume.
    -> questions

+ [When did you purchase this costume?]
    Shelly: Before Halloween, darling. I've got the receipt right here! I was so excited to dress as a bird. Had the whole outfit planned, too.
    -> questions

+ [Have you ever worn this costume?]
    Shelly: Oh, heavens, no! Couldn't even open the box after what happened. My sister says I should move on. But what doesn't kill you makes you... well, apparently it makes you drive all the way to a costume shop at this hour.
    -> questions

+ [Accuse of anomaly]
    -> accusations

+ [Finish questioning]
    -> END

=== accusations ===
+ {not accused_damage} [Accuse of slight damage]
    ~ accused_damage = true
    Shelly: Oh, goodness! Is something damaged? I haven't opened it, sweetheart. You're welcome to check. I'd never hurt a bird, not even a polyester one.
    -> accusations

+ {not accused_stain} [Accuse of a stain]
    ~ accused_stain = true
    Shelly: A stain? Oh, dear. It's been sealed up the whole time. Unless tears can soak through cardboard. Actually, don't answer that.
    -> accusations

+ {not accused_accessories} [Accuse of missing accessories]
    ~ accused_accessories = true
    Shelly: Both wings should be in there, darling! Left and right, just as advertised. Unlike a certain singing bird with a price tag that apparently wasn't for sale.
    -> accusations

+ {not accused_receipt} [Accuse of a missing receipt]
    ~ accused_receipt = true
    Shelly: Oh, I've got it right here! See? A proper receipt, with a proper price. Isn't it lovely when shops actually let you buy the things they put prices on?
    -> accusations

+ [Back to questions]
    -> questions

=== refund_response ===
Shelly: Oh, bless your heart! You've been so kind. Maybe there are still good little shops out there. I hope something absolutely wonderful happens to you tonight, sweetheart!
-> END

=== no_refund_response ===
Shelly: Oh. Even with the receipt? And the unopened box? Well, goodness. That's two shops in one week that don't seem to want my business. Have a lovely evening, sweetheart.
-> END
