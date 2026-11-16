import unittest
from step import parse


class Commands(unittest.TestCase):
    def test_quoted_labels_and_duplicate(self):
        self.assertEqual(parse('click "End the day" #2'),
                         dict(Verb='click', Label='End the day', Occurrence=2))
        self.assertEqual(parse('type "Name" "Morgan O\'Neil" #3'),
                         dict(Verb='type', Label='Name', Value="Morgan O'Neil", Occurrence=3))

    def test_invalid_command_never_reaches_driver(self):
        for command in ('', 'load private.json', 'click', 'key Enter', 'scroll Desk sideways', 'look extra'):
            with self.subTest(command=command), self.assertRaises(ValueError):
                parse(command)


if __name__ == '__main__':
    unittest.main()
