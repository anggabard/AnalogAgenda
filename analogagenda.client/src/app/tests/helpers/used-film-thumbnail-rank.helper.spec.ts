import { UsedFilmThumbnailDto } from '../../DTOs';
import { sortUsedFilmThumbnailsByBrandIsoSimilarity } from '../../helpers/used-film-thumbnail-rank.helper';

function t(filmName: string): UsedFilmThumbnailDto {
  return { id: filmName, filmName, imageId: '', imageUrl: '' };
}

describe('sortUsedFilmThumbnailsByBrandIsoSimilarity', () => {
  it('sorts alphabetically by filmName when brand is empty', () => {
    const sorted = sortUsedFilmThumbnailsByBrandIsoSimilarity(
      [t('Zebra'), t('Alpha'), t('Mike')],
      '   ',
      '400'
    );
    expect(sorted.map((x) => x.filmName)).toEqual(['Alpha', 'Mike', 'Zebra']);
  });

  it('puts brand prefix before substring before non-match', () => {
    const sorted = sortUsedFilmThumbnailsByBrandIsoSimilarity(
      [t('Other Kodak'), t('Kodak Portra 400'), t('Gold by Kodak')],
      'Kodak',
      '400'
    );
    expect(sorted.map((x) => x.filmName)).toEqual([
      'Kodak Portra 400',
      'Gold by Kodak',
      'Other Kodak'
    ]);
  });

  it('breaks ties by ISO distance when brand tier matches', () => {
    const sorted = sortUsedFilmThumbnailsByBrandIsoSimilarity(
      [t('Fuji 1600'), t('Fuji 400'), t('Fuji 800')],
      'Fuji',
      '400'
    );
    expect(sorted.map((x) => x.filmName)).toEqual(['Fuji 400', 'Fuji 800', 'Fuji 1600']);
  });

  it('uses minimum distance to ISO range endpoints', () => {
    const sorted = sortUsedFilmThumbnailsByBrandIsoSimilarity(
      [t('Brand 50'), t('Brand 250'), t('Brand 500')],
      'Brand',
      '200-400'
    );
    expect(sorted.map((x) => x.filmName)).toEqual(['Brand 250', 'Brand 500', 'Brand 50']);
  });

  it('uses localeCompare when brand and ISO scores tie', () => {
    const sorted = sortUsedFilmThumbnailsByBrandIsoSimilarity(
      [t('Ilford 400 B'), t('Ilford 400 A')],
      'Ilford',
      '400'
    );
    expect(sorted.map((x) => x.filmName)).toEqual(['Ilford 400 A', 'Ilford 400 B']);
  });

  it('sends thumbnails without digits last when ISO targets exist', () => {
    const sorted = sortUsedFilmThumbnailsByBrandIsoSimilarity(
      [t('Kodak no iso here'), t('Kodak 400')],
      'Kodak',
      '400'
    );
    expect(sorted.map((x) => x.filmName)).toEqual(['Kodak 400', 'Kodak no iso here']);
  });
});
