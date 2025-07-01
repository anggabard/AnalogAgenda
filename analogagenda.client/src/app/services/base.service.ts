import { environment } from "../../environments/environment";

export class BaseService {
    baseUrl: string;

    constructor(scope: string) {
        this.baseUrl= environment.apiUrl + '/' + scope;
    }
}