import { modalListMatches } from '../../helpers/modal-list-search.helper';

describe('modalListMatches', () => {
  it('returns true for empty query', () => {
    expect(modalListMatches('', 'anything')).toBeTrue();
    expect(modalListMatches('   ', 'x')).toBeTrue();
  });

  it('matches case-insensitively', () => {
    expect(modalListMatches('FOO', 'hello Foo bar')).toBeTrue();
    expect(modalListMatches('foo', 'FOO')).toBeTrue();
  });

  it('matches when any field contains the substring', () => {
    expect(modalListMatches('kodak', 'fuji', 'Kodak Gold', 'ilford')).toBeTrue();
    expect(modalListMatches('missing', 'a', 'b', 'c')).toBeFalse();
  });

  it('treats null and undefined fields as empty strings', () => {
    expect(modalListMatches('x', null, undefined, 'prefix xsuffix')).toBeTrue();
    expect(modalListMatches('x', null, undefined)).toBeFalse();
  });

  it('stringifies non-string field values (runtime)', () => {
    expect(modalListMatches('99', 'x', 1299 as unknown as string)).toBeTrue();
  });

  it('trims query before matching', () => {
    expect(modalListMatches('  foo  ', 'FooBar')).toBeTrue();
  });
});
