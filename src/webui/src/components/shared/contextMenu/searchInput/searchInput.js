// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { Svg } from "components/shared";
import { svgs } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./searchInput.module.scss"));

export const SearchInput = ({ children, className, ...rest }) => (
    <div className={css("context-menu-search-input", className)}>
        <Svg src={svgs.search} className={css("search-icon")} />
        <input className={css("search-text-box")} {...rest} type="text" />
    </div>
);
