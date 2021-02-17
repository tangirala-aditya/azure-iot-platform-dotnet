// Copyright (c) Microsoft. All rights reserved.

import { createStore, applyMiddleware } from "redux";
import { createEpicMiddleware } from "redux-observable";
import { composeWithDevTools } from "redux-devtools-extension";
import rootEpic from "./rootEpic";
import rootReducer from "./rootReducer";

export function configureStore() {
    // Initialize the redux-observable epics
    const epicMiddleware = createEpicMiddleware();

    // Initialize the redux store with middleware
    const store = createStore(
        rootReducer,
        composeWithDevTools(applyMiddleware(epicMiddleware))
    );

    epicMiddleware.run(rootEpic);
    return store;
}
