using System;
using System.Collections.Generic;
using System.Text;

namespace PokerCrosswords.App
{
	public class Crossword
	{
		private readonly List<List<Card>> _cards;
		public Crossword()
		{
			_cards = new List<List<Card>>(5);
			var deck = new Deck();
			for (var i = 0; i < 5; i++) {
				var handStrength = PickRandomHandStrength();
				while (!deck.Rig(handStrength))
					handStrength = PickRandomHandStrength();
				_cards.Add(new List<Card>(5));
				for (var j = 0; j < 5; j++)
					_cards[i].Add(deck.DrawOneCard());
			}
		}

		private static readonly Lazy<Random> Random = new Lazy<Random>(() => new Random());
		private static readonly IReadOnlyList<HandStrength> ReasonablyStrongHands = new[] {
			HandStrength.StraightFlush,
			HandStrength.FourOfAKind,
			HandStrength.Flush,
			HandStrength.FullHouse,
			HandStrength.Straight
		};

		private static HandStrength PickRandomHandStrength() => ReasonablyStrongHands[Random.Value.Next(ReasonablyStrongHands.Count)];

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach (var row in _cards) {
				sb.AppendLine();
				foreach (var card in row) {
					var str = card.ToString();
					sb.Append(str.Length == 2 ? str + "  " : str + " ");
				}
				sb.Append($" <-- {new Hand(row).GetHandStrength()}");
				sb.AppendLine();
			}
			return sb.ToString();
		}
	}
}