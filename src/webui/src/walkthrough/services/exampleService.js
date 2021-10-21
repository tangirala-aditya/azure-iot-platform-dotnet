// Copyright (c) Microsoft. All rights reserved.

// <service>

import { of } from "rxjs";
import { delay, map } from "rxjs/operators";
import { toExampleItemModel, toExampleItemsModel } from "./models";

/** Normally, you'll need to define the endpoint URL.
 * See app.config.js to add a new service URL.
 *
 * For this example, we'll just hardcode sample data to be returned instead
 * of making an actual service call. See the other service files for examples.
 */
//const ENDPOINT = Config.serviceUrls.example;

/** Contains methods for calling the example service */
export class ExampleService {
    /** Returns an example item */
    static getExampleItem(id) {
        return of({
            ID: id,
            Description: "This is an example item.",
        }).pipe(map(toExampleItemModel));
    }

    /** Returns a list of example items */
    static getExampleItems() {
        return of({
            items: [
                { ID: "123", Description: "This is item 123." },
                { ID: "188", Description: "This is item ONE-DOUBLE-EIGHT." },
                { ID: "210", Description: "This is item TWO-TEN." },
                { ID: "277", Description: "This is item 277." },
                { ID: "413", Description: "This is item FOUR-THIRTEEN." },
                { ID: "789", Description: "This is item 789." },
            ],
        }).pipe(map(toExampleItemsModel));
    }

    /** Mimics a server call by adding a delay */
    static updateExampleItems() {
        return this.getExampleItems().pipe(delay(2000));
    }
}
// </service>
