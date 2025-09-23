import { Observable } from "rxjs";
import { BaseService } from "./base.service";
import { PagedResponseDto } from "../DTOs";

/**
 * Base service class that provides common CRUD and pagination patterns
 */
export abstract class BasePaginatedService<TDto> extends BaseService {
    
    constructor(scope: string) {
        super(scope);
    }

    // Common CRUD operations
    add(item: TDto): Observable<any> {
        return this.post('', item);
    }

    getAll(): Observable<TDto[]> {
        return this.get<TDto[]>('?page=0'); // page=0 for backward compatibility
    }

    getById(id: string): Observable<TDto> {
        return this.get<TDto>(id);
    }

    update(id: string, item: TDto): Observable<any> {
        return this.put(id, item);
    }

    deleteById(id: string): Observable<any> {
        return super.delete(id);
    }

    // Common pagination operations
    getPaged(page: number = 1, pageSize: number = 5): Observable<PagedResponseDto<TDto>> {
        return this.get<PagedResponseDto<TDto>>(`?page=${page}&pageSize=${pageSize}`);
    }

    // Helper method for building query parameters
    protected buildPagedQuery(page: number, pageSize: number, additionalParams?: Record<string, any>): string {
        const params = new URLSearchParams({
            page: page.toString(),
            pageSize: pageSize.toString(),
            ...(additionalParams || {})
        });
        return `?${params.toString()}`;
    }

    // Helper method for building filtered paged endpoints
    protected getFilteredPaged(
        filter: string, 
        page: number = 1, 
        pageSize: number = 5,
        additionalParams?: Record<string, any>
    ): Observable<PagedResponseDto<TDto>> {
        const query = this.buildPagedQuery(page, pageSize, additionalParams);
        return this.get<PagedResponseDto<TDto>>(`${filter}${query}`);
    }
}
